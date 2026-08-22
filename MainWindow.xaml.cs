using System;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Controls;
using Microsoft.Win32;

namespace ElsEvo
{
    public partial class MainWindow : Window
    {
        private bool _modsLigados;
        private CancellationTokenSource? _cancelamentoAtual;
        private GerenciadorBandeja? _bandeja;

        private double _larguraAntesDeMaximizar;
        private double _alturaAntesDeMaximizar;
        private double _topoAntesDeMaximizar;
        private double _esquerdaAntesDeMaximizar;
        private bool _estaMaximizada;

        public MainWindow()
        {
            InitializeComponent();
            _modsLigados = Properties.Settings.Default.ModsEnabled;

            Idiomas.IdiomaMudou += AplicarIdioma;
            ThemeManager.TemaMudou += _ => AtualizarVisualToggle();

            Loaded += (_, _) =>
            {
                ThemeManager.AplicarTemaSalvo();
                AtualizarListaDeModsAtivos();
                AtualizarVisualToggle();
                AplicarIdioma();
                ConfigurarBandeja();
                BadgeBeta.Visibility = Properties.Settings.Default.IsBetaRelease ? Visibility.Visible : Visibility.Collapsed;

                // Se o app acabou de reabrir sozinho por causa de um update (ver
                // ReabrirAppAtualizadoEFechar, que passa esse argumento), mostra a
                // confirmação de sucesso em vez de checar atualização de novo — checar de
                // novo logo em seguida seria redundante (a gente já sabe que atualizou).
                bool acabouDeAtualizar = Environment.GetCommandLineArgs()
                    .Any(arg => arg.Equals("--atualizado", StringComparison.OrdinalIgnoreCase));

                if (acabouDeAtualizar)
                {
                    var janelaSucesso = new AtualizacaoConcluidaWindow { Owner = this };
                    janelaSucesso.ShowDialog();
                }
                else
                {
                    _ = VerificarAtualizacaoAsync();
                }
            };

            Closing += MainWindow_Closing;
        }

        private void ConfigurarBandeja()
        {
            string caminhoExe = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
            if (!string.IsNullOrEmpty(caminhoExe))
                _bandeja = new GerenciadorBandeja(this, caminhoExe);
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Properties.Settings.Default.MinimizarParaBandeja && _bandeja != null)
            {
                e.Cancel = true;
                Hide();
                _bandeja.Mostrar();
            }
            else
            {
                _bandeja?.Dispose();
            }
        }

        // ===================== IDIOMA =====================

        private void AplicarIdioma()
        {
            MenuItemAcoes.Header = Idiomas.T("MenuAcoes");
            MenuItemConfiguracoes.Header = Idiomas.T("MenuConfiguracoes");
            MenuItemSobre.Header = Idiomas.T("MenuSobre");
            ItemReiniciar.Header = Idiomas.T("AcaoReiniciar");
            ItemLimparCache.Header = Idiomas.T("AcaoLimparCache");
            ItemLimparConfiguracoes.Header = Idiomas.T("AcaoLimparConfiguracoes");
            ItemExcluirMods.Header = Idiomas.T("AcaoExcluirMods");
            BtnGerenciarMods.Content = Idiomas.T("BtnGerenciarMods");
            TxtModsAtivos.Text = Idiomas.T("ModsAtivos");
            TxtListaVazia.Text = Idiomas.T("ListaVazia");
            StatusBadge.Text = _modsLigados ? Idiomas.T("Ligado") : Idiomas.T("Desligado");
            AtualizarTextoBotaoJogar();
        }

        // ===================== MODS ATIVOS (agrupado por pack) =====================

        private void AtualizarListaDeModsAtivos()
        {
            var ativos = GerenciadorDeMods.Carregar();
            ListaModsAtivos.Items.Clear();

            var porPack = ativos.GroupBy(m => m.NomeDoPack);
            foreach (var grupo in porPack)
            {
                int quantidade = grupo.Count();
                var item = new ListBoxItem
                {
                    Padding = new Thickness(6),
                    Content = quantidade == 1 ? grupo.Key : $"{grupo.Key}  ({quantidade} arquivos)"
                };
                ListaModsAtivos.Items.Add(item);
            }

            bool temMods = ativos.Count > 0;
            ListaModsAtivos.Visibility = temMods ? Visibility.Visible : Visibility.Collapsed;
            TxtListaVazia.Visibility = temMods ? Visibility.Collapsed : Visibility.Visible;
        }

        // ===================== BARRA DE TÍTULO CUSTOM =====================

        private void BarraTitulo_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                BtnMaximizar_Click(sender, e);
                return;
            }

            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void BtnMinimizar_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void BtnMaximizar_Click(object sender, RoutedEventArgs e)
        {
            if (!_estaMaximizada)
            {
                _larguraAntesDeMaximizar = Width;
                _alturaAntesDeMaximizar = Height;
                _topoAntesDeMaximizar = Top;
                _esquerdaAntesDeMaximizar = Left;

                var areaUtil = SystemParameters.WorkArea;
                Left = areaUtil.Left;
                Top = areaUtil.Top;
                Width = areaUtil.Width;
                Height = areaUtil.Height;

                _estaMaximizada = true;
            }
            else
            {
                Width = _larguraAntesDeMaximizar;
                Height = _alturaAntesDeMaximizar;
                Top = _topoAntesDeMaximizar;
                Left = _esquerdaAntesDeMaximizar;

                _estaMaximizada = false;
            }
        }

        private void Window_StateChanged(object? sender, EventArgs e)
        {
            if (WindowState == WindowState.Maximized)
                WindowState = WindowState.Normal;
        }

        private void BtnFechar_Click(object sender, RoutedEventArgs e) => Close();

        // ===================== TOGGLE LIGADO/DESLIGADO (ModsEnabled) =====================

        private void BtnToggleLigado_Click(object sender, RoutedEventArgs e)
        {
            _modsLigados = !_modsLigados;
            AtualizarVisualToggle();

            var cfg = Properties.Settings.Default;
            cfg.ModsEnabled = _modsLigados;
            cfg.Save();
        }

        private void AtualizarVisualToggle()
        {
            var bc = new BrushConverter();
            bool temaClaro = Properties.Settings.Default.TemaClaro;

            if (_modsLigados)
            {
                StatusBadge.Text = Idiomas.T("Ligado");
                if (temaClaro)
                {
                    StatusBadge.Foreground = Brushes.White;
                    BadgeBorder.Background = (Brush)bc.ConvertFrom("#2E7D32")!;
                    BadgeBorder.BorderThickness = new Thickness(0);
                }
                else
                {
                    StatusBadge.Foreground = (Brush)bc.ConvertFrom("#69E292")!;
                    BadgeBorder.Background = (Brush)bc.ConvertFrom("#263D30")!;
                    BadgeBorder.BorderBrush = (Brush)bc.ConvertFrom("#4E9F6D")!;
                    BadgeBorder.BorderThickness = new Thickness(1);
                }
            }
            else
            {
                StatusBadge.Text = Idiomas.T("Desligado");
                if (temaClaro)
                {
                    StatusBadge.Foreground = Brushes.White;
                    BadgeBorder.Background = (Brush)bc.ConvertFrom("#C62828")!;
                    BadgeBorder.BorderThickness = new Thickness(0);
                }
                else
                {
                    StatusBadge.Foreground = (Brush)bc.ConvertFrom("#F28B82")!;
                    BadgeBorder.Background = (Brush)bc.ConvertFrom("#3D2626")!;
                    BadgeBorder.BorderBrush = (Brush)bc.ConvertFrom("#9F4E4E")!;
                    BadgeBorder.BorderThickness = new Thickness(1);
                }
            }

            AtualizarCorBadgeBeta();
            AtualizarTextoBotaoJogar();
        }

        private void AtualizarTextoBotaoJogar()
        {
            BtnJogar.Content = _modsLigados ? Idiomas.T("BtnAplicarJogar") : Idiomas.T("BtnExecutarLauncher");
        }

        private void AtualizarCorBadgeBeta()
        {
            var bc = new BrushConverter();
            bool temaClaro = Properties.Settings.Default.TemaClaro;

            if (temaClaro)
            {
                BadgeBeta.Background = (Brush)bc.ConvertFrom("#D32F2F")!;
                BadgeBeta.BorderThickness = new Thickness(0);
                ((TextBlock)BadgeBeta.Child).Foreground = Brushes.White;
            }
            else
            {
                BadgeBeta.Background = (Brush)bc.ConvertFrom("#3D2F1E")!;
                BadgeBeta.BorderBrush = (Brush)bc.ConvertFrom("#B8860B")!;
                BadgeBeta.BorderThickness = new Thickness(1);
                ((TextBlock)BadgeBeta.Child).Foreground = (Brush)bc.ConvertFrom("#E0B060")!;
            }
        }

        // ===================== MENU: AÇÕES =====================

        private void MenuReiniciar_Click(object sender, RoutedEventArgs e)
        {
            string caminhoExeAtual = Process.GetCurrentProcess().MainModule?.FileName
                                      ?? Environment.ProcessPath
                                      ?? string.Empty;

            if (!string.IsNullOrEmpty(caminhoExeAtual))
                Process.Start(caminhoExeAtual);

            Application.Current.Shutdown();
        }

        /// <summary>
        /// Limpa SÓ o cache temporário de patch (Paths.Main.Cache — pasta "gPatcher cache"
        /// na raiz do disco, usada durante o fluxo de aplicar mods). NÃO mexe em nenhuma
        /// configuração do app (isso é responsabilidade exclusiva de
        /// MenuLimparConfiguracoes_Click, ver comentário lá).
        ///
        /// Tenta apagar a pasta inteira de uma vez (mais rápido e evita deixar restos de
        /// subpastas). Se isso falhar (ex.: algum arquivo dentro está em uso — comum
        /// durante desenvolvimento, com o VS Code/dotnet ainda segurando um handle de uma
        /// execução anterior), cai pro modo tolerante: apaga arquivo por arquivo e pasta
        /// por pasta individualmente, ignorando silenciosamente qualquer item que esteja
        /// bloqueado, em vez de abortar tudo com um único arquivo preso.
        /// </summary>
        private void MenuLimparCache_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string pastaCache = Paths.Main.Cache;

                bool limpezaCompleta = TentarApagarPastaInteira(pastaCache);

                if (!limpezaCompleta)
                    limpezaCompleta = ApagarConteudoTolerandoArquivosTravados(pastaCache);

                Directory.CreateDirectory(pastaCache);

                if (limpezaCompleta)
                {
                    JanelaConfirmacao.Mostrar(this,
                        "Cache limpo",
                        "O cache de arquivos temporários foi limpo com sucesso.",
                        TipoMensagem.Sucesso);
                }
                else
                {
                    JanelaConfirmacao.Mostrar(this,
                        "Cache parcialmente limpo",
                        "A maior parte do cache foi limpa, mas alguns arquivos estavam em uso " +
                        "por outro programa (ex.: o jogo ou uma execução em andamento) e não " +
                        "puderam ser removidos agora. Feche esses programas e tente novamente " +
                        "se quiser limpar tudo.",
                        TipoMensagem.Aviso);
                }
            }
            catch (Exception ex)
            {
                JanelaConfirmacao.Mostrar(this,
                    "Erro ao limpar cache",
                    $"Não foi possível limpar o cache:\n{ex.Message}",
                    TipoMensagem.Erro);
            }
        }

        /// <summary>Tenta apagar a pasta de cache inteira de uma vez. Retorna false (sem
        /// lançar exceção) se falhar por permissão/arquivo em uso, pra quem chamou decidir
        /// cair pro modo tolerante em vez de travar o app inteiro.</summary>
        private static bool TentarApagarPastaInteira(string pastaCache)
        {
            try
            {
                if (Directory.Exists(pastaCache))
                    Directory.Delete(pastaCache, recursive: true);
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (IOException)
            {
                return false;
            }
        }

        /// <summary>Apaga o conteúdo da pasta item por item (arquivos e subpastas),
        /// ignorando qualquer item que esteja travado por outro processo. Retorna true
        /// somente se TUDO foi removido com sucesso.</summary>
        private static bool ApagarConteudoTolerandoArquivosTravados(string pastaCache)
        {
            if (!Directory.Exists(pastaCache))
                return true;

            bool tudoRemovido = true;

            foreach (var arquivo in Directory.GetFiles(pastaCache, "*", SearchOption.AllDirectories))
            {
                try { File.Delete(arquivo); }
                catch { tudoRemovido = false; }
            }

            // Depois de apagar os arquivos, tenta remover as subpastas que ficaram vazias
            // (de trás pra frente, pra remover as mais profundas primeiro).
            var subpastas = Directory.GetDirectories(pastaCache, "*", SearchOption.AllDirectories)
                .OrderByDescending(p => p.Length);

            foreach (var subpasta in subpastas)
            {
                try
                {
                    if (!Directory.EnumerateFileSystemEntries(subpasta).Any())
                        Directory.Delete(subpasta);
                    else
                        tudoRemovido = false;
                }
                catch { tudoRemovido = false; }
            }

            return tudoRemovido;
        }

        /// <summary>
        /// Restaura SÓ as configurações do app (Properties.Settings.Default.Reset()) —
        /// NÃO apaga nem mexe em nenhum arquivo em disco (nem cache, nem packs de mod).
        /// Antes esse reset também deixava Paths.Main.Cache "órfão" (porque zera
        /// ElswordDirectory, e a pasta de cache é calculada a partir dele) — dando a
        /// falsa impressão de que "limpar configurações" também limpava o cache. Isso
        /// continua reiniciando ElswordDirectory (é o comportamento esperado de um
        /// reset de configurações), mas agora o cache tem seu próprio botão dedicado e
        /// robusto (ver MenuLimparCache_Click), então não há mais ambiguidade sobre qual
        /// botão faz o quê.
        /// </summary>
        private void MenuLimparConfiguracoes_Click(object sender, RoutedEventArgs e)
        {
            bool confirmou = JanelaConfirmacao.Confirmar(this,
                "Limpar configurações",
                "Isso vai restaurar todas as configurações do ElsEvo para o padrão. Continuar?",
                TipoMensagem.Aviso);

            if (!confirmou)
                return;

            Properties.Settings.Default.Reset();
            Properties.Settings.Default.Save();

            ThemeManager.AplicarTemaSalvo();
            InicializacaoComWindows.Aplicar(Properties.Settings.Default.IniciarComWindows);
            AplicarIdioma();

            JanelaConfirmacao.Mostrar(this,
                "Configurações restauradas",
                "Todas as configurações do ElsEvo foram restauradas para o padrão.",
                TipoMensagem.Sucesso);
        }

        private void MenuExcluirMods_Click(object sender, RoutedEventArgs e)
        {
            bool confirmou = JanelaConfirmacao.Confirmar(this,
                "Excluir todos os mods",
                "Isso vai excluir TODOS os packs de mods importados. Essa ação não pode ser desfeita. Continuar?",
                TipoMensagem.Aviso);

            if (!confirmou)
                return;

            try
            {
                string pastaPacks = Paths.Main.Packs;

                bool exclusaoCompleta = TentarApagarPastaInteira(pastaPacks);

                if (!exclusaoCompleta)
                    exclusaoCompleta = ApagarConteudoTolerandoArquivosTravados(pastaPacks);

                Directory.CreateDirectory(pastaPacks);

                GerenciadorDeMods.Salvar(new List<ModAtivo>());
                AtualizarListaDeModsAtivos();

                if (exclusaoCompleta)
                {
                    JanelaConfirmacao.Mostrar(this,
                        "Mods excluídos",
                        "Todos os mods foram excluídos.",
                        TipoMensagem.Sucesso);
                }
                else
                {
                    JanelaConfirmacao.Mostrar(this,
                        "Mods parcialmente excluídos",
                        "A maior parte dos mods foi excluída, mas alguns arquivos estavam em uso " +
                        "por outro programa (ex.: o jogo ou uma execução em andamento) e não " +
                        "puderam ser removidos agora. Feche esses programas e tente novamente " +
                        "se quiser excluir tudo.",
                        TipoMensagem.Aviso);
                }
            }
            catch (Exception ex)
            {
                JanelaConfirmacao.Mostrar(this,
                    "Erro ao excluir mods",
                    $"Não foi possível excluir os mods:\n{ex.Message}",
                    TipoMensagem.Erro);
            }
        }

        // ===================== MENU: CONFIGURAÇÕES / SOBRE =====================

        private void MenuConfiguracoes_Click(object sender, RoutedEventArgs e)
        {
            var janela = new PreferenciasWindow { Owner = this };
            janela.ShowDialog();

            BadgeBeta.Visibility = Properties.Settings.Default.IsBetaRelease ? Visibility.Visible : Visibility.Collapsed;
            AtualizarVisualToggle();
            AplicarIdioma();
        }

        private void MenuSobre_Click(object sender, RoutedEventArgs e)
        {
            var janela = new SobreWindow { Owner = this };
            janela.ShowDialog();
        }

        private void BtnGerenciarMods_Click(object sender, RoutedEventArgs e)
        {
            var janela = new GerenciarModsWindow { Owner = this };
            janela.ShowDialog();

            AtualizarListaDeModsAtivos();
        }

        // ===================== APLICAR E JOGAR =====================

        private async void BtnJogar_Click(object sender, RoutedEventArgs e)
        {
            if (!Paths.Elsword.IsValidElswordDir(Properties.Settings.Default.ElswordDirectory))
            {
                JanelaConfirmacao.Mostrar(this,
                    "Pasta do jogo inválida",
                    "A pasta do Elsword configurada não é válida (precisa ter \"elsword.exe\" e a pasta \"data\").\n" +
                    "Configure em Configurações → Elsword → Localização do jogo.",
                    TipoMensagem.Aviso);
                return;
            }

            var listaDePatches = new List<PatchInfo>();

            if (_modsLigados)
            {
                var ativos = GerenciadorDeMods.Carregar();
                listaDePatches = ativos
                    .Where(m => File.Exists(m.CaminhoCompleto))
                    .Select(m => new PatchInfo(m))
                    .ToList();
            }

            BtnJogar.IsEnabled = false;
            BtnJogar.Content = "Aguardando o launcher...";

            ProgressoContainer.Visibility = Visibility.Visible;
            BarraProgresso.Value = 0;
            TxtProgresso.Text = "0%";

            var progresso = new Progress<int>(percentual =>
            {
                BarraProgresso.Value = percentual;
                TxtProgresso.Text = $"{percentual}%";
            });

            var statusProgresso = new Progress<EstadoPatch>(estado =>
            {
                BtnJogar.Content = estado switch
                {
                    EstadoPatch.PreparandoArquivos => "Preparando arquivos...",
                    EstadoPatch.AguardandoElswordAbrir => "Aguardando o launcher fechar...",
                    EstadoPatch.FazendoBackup => "Fazendo backup...",
                    EstadoPatch.Aplicando => "Aplicando mods...",
                    EstadoPatch.AguardandoElswordFechar => "Mods ativos — divirta-se! 🎮",
                    EstadoPatch.RestaurandoBackup => "Restaurando backup...",
                    _ => "Concluído"
                };
            });

            _cancelamentoAtual = new CancellationTokenSource();

            try
            {
                await PatcherService.ExecutarFluxoPatchAsync(
                    listaDePatches, progresso, statusProgresso, _cancelamentoAtual.Token);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                JanelaConfirmacao.Mostrar(this,
                    "Erro durante o patch",
                    $"Ocorreu um erro durante o patch:\n{ex.Message}",
                    TipoMensagem.Erro);
            }
            finally
            {
                BtnJogar.IsEnabled = true;
                AtualizarTextoBotaoJogar();
                ProgressoContainer.Visibility = Visibility.Collapsed;
                _cancelamentoAtual = null;
            }
        }

        // ===================== ATUALIZAÇÃO AUTOMÁTICA =====================

        /// <summary>
        /// Roda no Loaded da janela (se "Buscar atualizações ao iniciar" estiver marcado).
        /// Checa o version.json remoto (respeitando o canal estável/beta escolhido) e, se
        /// achar uma versão mais nova, pergunta ao usuário se quer baixar e instalar agora.
        /// Qualquer falha na CHECAGEM em si é sempre silenciosa (ver AtualizacaoService) —
        /// não faz sentido incomodar o usuário toda vez que abrir o app sem internet.
        /// </summary>
        private async Task VerificarAtualizacaoAsync()
        {
            if (!Properties.Settings.Default.CheckForProgramUpdates)
                return;

            var atualizacao = await AtualizacaoService.VerificarAsync();
            if (atualizacao == null)
                return;

            // Janela customizada seguindo o tema do app, em vez do MessageBox nativo do
            // Windows — ver AtualizacaoWindow.xaml. DialogResult == true = "atualizar agora".
            var janela = new AtualizacaoWindow(atualizacao) { Owner = this };
            bool? resposta = janela.ShowDialog();

            if (resposta != true)
                return;

            await BaixarEInstalarAtualizacaoAsync(atualizacao);
        }

        /// <summary>
        /// Baixa o instalador (.exe do Inno Setup) pra uma pasta temporária, mostrando
        /// progresso na mesma barra usada pelo "Aplicar e Jogar". Ao terminar, roda o
        /// instalador em modo SILENCIOSO (sem assistente visível, sem exigir clique
        /// nenhum do usuário), espera ele terminar de verdade, e então reabre o ElsEvo
        /// sozinho já na versão nova. Erros de rede durante o download OU falha no
        /// instalador silencioso são tratados sem deixar o usuário no escuro: mostra um
        /// aviso visível (já que sem assistente ele não veria nada acontecer sozinho).
        /// </summary>
        private async Task BaixarEInstalarAtualizacaoAsync(AtualizacaoDisponivel atualizacao)
        {
            string caminhoInstalador = Path.Combine(Path.GetTempPath(), "ElsEvo-Setup.exe");

            // Bloqueia as ações principais enquanto a atualização roda — o usuário não
            // pode clicar em "Aplicar e Jogar" (ou abrir "Gerenciar Mods") no meio do
            // download/instalação, pra não arriscar mexer em arquivos ao mesmo tempo que
            // o instalador. Reabilitado no finally, cobrindo TODO caminho de saída
            // (sucesso, erro de rede, erro do instalador) — só não reabilita se o app já
            // tiver sido fechado com sucesso (Application.Current.Shutdown()).
            BtnJogar.IsEnabled = false;
            BtnGerenciarMods.IsEnabled = false;
            bool appVaiFecharComSucesso = false;

            try
            {
                ProgressoContainer.Visibility = Visibility.Visible;
                BarraProgresso.Value = 0;
                TxtProgresso.Text = "Baixando atualização... 0%";

                bool baixouComSucesso = false;

                try
                {
                    using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
                    using var resposta = await http.GetAsync(atualizacao.UrlInstalador, HttpCompletionOption.ResponseHeadersRead);
                    resposta.EnsureSuccessStatusCode();

                    long? tamanhoTotal = resposta.Content.Headers.ContentLength;

                    await using var streamOrigem = await resposta.Content.ReadAsStreamAsync();
                    await using var streamDestino = File.Create(caminhoInstalador);

                    var buffer = new byte[81920];
                    long totalLido = 0;
                    int lido;

                    while ((lido = await streamOrigem.ReadAsync(buffer)) > 0)
                    {
                        await streamDestino.WriteAsync(buffer.AsMemory(0, lido));
                        totalLido += lido;

                        if (tamanhoTotal is > 0)
                        {
                            int percentual = (int)(totalLido * 100 / tamanhoTotal.Value);
                            BarraProgresso.Value = percentual;
                            TxtProgresso.Text = $"Baixando atualização... {percentual}%";
                        }
                    }

                    baixouComSucesso = true;
                }
                catch (Exception ex)
                {
                    JanelaConfirmacao.Mostrar(this,
                        "Falha ao atualizar",
                        $"Não foi possível baixar a atualização automaticamente:\n{ex.Message}\n\n" +
                        "O ElsEvo vai continuar funcionando normalmente na versão atual. Você pode " +
                        "tentar de novo mais tarde, ou baixar manualmente pela página de Releases no GitHub.",
                        TipoMensagem.Aviso);
                }

                if (!baixouComSucesso)
                    return;

                // A partir daqui a barra continua visível, agora mostrando "Instalando..." —
                // o usuário não interage com o instalador (roda sem assistente), então esse
                // texto na própria janela do ElsEvo é o único feedback visual que ele tem.
                // Como o Inno Setup silencioso não reporta progresso real de volta, a barra
                // fica travada em 100% — pra não parecer que travou/quebrou, os pontinhos no
                // final do texto animam sozinhos enquanto espera (só isso muda, a barra não).
                BarraProgresso.Value = 100;

                var timerPontinhos = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(450)
                };
                int quantidadePontos = 0;
                timerPontinhos.Tick += (_, _) =>
                {
                    quantidadePontos = (quantidadePontos + 1) % 4;
                    TxtProgresso.Text = "Instalando atualização, aguarde" + new string('.', quantidadePontos);
                };
                timerPontinhos.Start();

                int codigoSaida;
                try
                {
                    // /VERYSILENT: instala sem NENHUMA janela do assistente (nem barra de
                    // progresso própria do Inno Setup — por isso mostramos a nossa).
                    // /SUPPRESSMSGBOXES: qualquer caixa de diálogo do instalador (avisos,
                    // confirmações) é respondida automaticamente com a opção padrão, sem
                    // travar esperando clique.
                    // /NORESTART: nunca reinicia o Windows sozinho, mesmo que ache necessário.
                    // /SP-: pula a telinha inicial (irrelevante em modo silencioso, mas
                    // mantido por consistência com o fluxo anterior).
                    //
                    // ATENÇÃO: como o destino padrão é Program Files, o Windows exige
                    // elevação — o UAC ("Deseja permitir que este app faça alterações no
                    // dispositivo?") ainda aparece mesmo com /VERYSILENT, isso é decisão
                    // do Windows, não do instalador, e não dá pra suprimir por código.
                    var processoInstalador = Process.Start(new ProcessStartInfo
                    {
                        FileName = caminhoInstalador,
                        Arguments = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-",
                        UseShellExecute = true
                    });

                    if (processoInstalador == null)
                        throw new InvalidOperationException("Não foi possível iniciar o processo do instalador.");

                    // Espera o instalador terminar DE VERDADE antes de continuar — sem isso o
                    // app fecharia ou tentaria reabrir antes dos arquivos serem substituídos.
                    await Task.Run(() => processoInstalador.WaitForExit());
                    codigoSaida = processoInstalador.ExitCode;
                }
                catch (Exception ex)
                {
                    timerPontinhos.Stop();
                    JanelaConfirmacao.Mostrar(this,
                        "Falha ao iniciar instalador",
                        $"O instalador foi baixado, mas não foi possível executá-lo automaticamente:\n{ex.Message}\n\n" +
                        $"Você pode rodar ele manualmente em:\n{caminhoInstalador}",
                        TipoMensagem.Aviso);
                    return;
                }

                timerPontinhos.Stop();

                // Código de saída do Inno Setup: 0 = sucesso. Qualquer outro valor indica que
                // algo deu errado (ex.: 5 = falha na instalação, 6 = cancelado pelo Restart
                // Manager) — como não tem assistente visível, o usuário não veria isso sozinho,
                // então mostramos um aviso explícito em vez de simplesmente reabrir o app.
                if (codigoSaida != 0)
                {
                    JanelaConfirmacao.Mostrar(this,
                        "Atenção — instalação da atualização",
                        $"O instalador terminou com um erro (código {codigoSaida}) e a atualização pode não " +
                        "ter sido concluída corretamente.\n\n" +
                        "O ElsEvo vai continuar/reabrir normalmente. Se algo parecer errado, tente " +
                        "baixar e instalar manualmente pela página de Releases no GitHub.",
                        TipoMensagem.Aviso);
                }

                // A partir daqui o app vai fechar de propósito (ReabrirAppAtualizadoEFechar
                // chama Application.Current.Shutdown() no final) — não faz sentido reabilitar
                // os botões nesse caminho, a janela já não vai mais existir.
                appVaiFecharComSucesso = true;
                ReabrirAppAtualizadoEFechar();
            }
            finally
            {
                if (!appVaiFecharComSucesso)
                {
                    ProgressoContainer.Visibility = Visibility.Collapsed;
                    BtnJogar.IsEnabled = true;
                    BtnGerenciarMods.IsEnabled = true;
                }
            }
        }

        /// <summary>
        /// Reabre o ElsEvo sozinho (já na versão instalada pelo update) e fecha esta
        /// instância antiga em seguida — o usuário não precisa clicar em nada. O caminho
        /// do .exe pós-instalação é lido do registro do Windows (chave de desinstalação
        /// criada pelo Inno Setup, identificada pelo AppId fixo do ElsEvo.iss — o MESMO
        /// AppId nos dois canais, estável e beta), porque é a forma confiável de saber
        /// onde ficou instalado. Se não achar no registro por qualquer motivo, cai pro
        /// caminho do processo atual como último recurso.
        ///
        /// NOTA sobre o bug "não reabre sozinho": o ElsEvo.iss tinha
        /// "RestartApplications=yes" além de "CloseApplications=yes". Isso fazia o
        /// próprio Windows/Inno Setup (via Restart Manager) tentar reabrir o app
        /// automaticamente DEPOIS da instalação — competindo com esse método, que também
        /// reabre manualmente. Os dois mecanismos disputando (um pelo caminho certo via
        /// registro, o outro pelo registro do Restart Manager, que pode referenciar um
        /// processo/caminho zumbi) explica o comportamento observado: às vezes reabre,
        /// às vezes fica em silêncio sem erro nenhum. "RestartApplications" foi removido
        /// do .iss — esse método aqui é o ÚNICO responsável por reabrir o app agora.
        /// </summary>
        private void ReabrirAppAtualizadoEFechar()
        {
            string? caminhoRegistro = ObterCaminhoExeInstalado();
            string? caminhoProcessoAtual = Process.GetCurrentProcess().MainModule?.FileName;
            string? caminhoExeNovo = caminhoRegistro ?? caminhoProcessoAtual;

            // Log de diagnóstico temporário — grava em %LocalAppData%\ElsEvo\update-log.txt
            // exatamente o que essa etapa encontrou/fez, porque a janela de erro (se
            // aparecer) pode passar rápido demais na tela durante o fechamento do app.
            // Não precisa de nenhuma configuração pra funcionar; se a escrita falhar por
            // qualquer motivo, ignora silenciosamente (não é crítico pro fluxo em si).
            void Log(string linha)
            {
                try
                {
                    Directory.CreateDirectory(Paths.LocalApplicationData);
                    string caminhoLog = Path.Combine(Paths.LocalApplicationData, "update-log.txt");
                    File.AppendAllText(caminhoLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {linha}\n");
                }
                catch { }
            }

            Log("===== Iniciando ReabrirAppAtualizadoEFechar (canal BETA) =====");
            Log($"Caminho via registro: {caminhoRegistro ?? "(não encontrado)"}");
            Log($"Caminho do processo atual: {caminhoProcessoAtual ?? "(nulo)"}");
            Log($"Caminho escolhido: {caminhoExeNovo ?? "(nenhum)"}");
            Log($"Arquivo existe? {(!string.IsNullOrEmpty(caminhoExeNovo) && File.Exists(caminhoExeNovo))}");

            try
            {
                if (!string.IsNullOrEmpty(caminhoExeNovo) && File.Exists(caminhoExeNovo))
                {
                    var processoNovo = Process.Start(new ProcessStartInfo
                    {
                        FileName = caminhoExeNovo,
                        Arguments = "--atualizado",
                        UseShellExecute = true
                    });

                    Log(processoNovo != null
                        ? $"Process.Start retornou um processo válido (Id={processoNovo.Id})."
                        : "Process.Start retornou null (nenhuma exceção lançada).");
                }
                else
                {
                    Log("Nenhum caminho válido encontrado — mostrando aviso pro usuário.");
                    MessageBox.Show(
                        "A atualização foi instalada, mas não foi possível localizar o executável " +
                        "novo para reabrir automaticamente. Abra o ElsEvo manualmente.",
                        "ElsEvo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Log($"EXCEÇÃO ao tentar reabrir: {ex}");
                MessageBox.Show(
                    $"A atualização foi instalada, mas não foi possível reabrir o ElsEvo automaticamente:\n{ex.Message}\n\n" +
                    "Abra o ElsEvo manualmente.",
                    "ElsEvo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            finally
            {
                Log("Chamando Application.Current.Shutdown().");
                Application.Current.Shutdown();
            }
        }

        /// <summary>
        /// Lê a pasta de instalação real do ElsEvo a partir da chave de desinstalação que
        /// o Inno Setup cria no registro (identificada pelo AppId fixo definido no
        /// .iss — {8910440C-BF7A-494D-B5AD-7F0A4DA85D60}, IDÊNTICO nos dois canais). Checa
        /// tanto a visão de 64 bits quanto a WOW6432Node (32 bits), e HKLM antes de HKCU,
        /// cobrindo tanto instalação padrão (todos os usuários) quanto "somente usuário
        /// atual". Procura pelo executável já com o nome novo pós-rename (ElsEvo.exe).
        /// </summary>
        private static string? ObterCaminhoExeInstalado()
        {
            const string chaveAppId = @"{8910440C-BF7A-494D-B5AD-7F0A4DA85D60}_is1";

            string[] subchaves =
            {
                $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{chaveAppId}",
                $@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\{chaveAppId}"
            };

            foreach (var raiz in new[] { Registry.LocalMachine, Registry.CurrentUser })
            {
                foreach (var subchave in subchaves)
                {
                    try
                    {
                        using var chave = raiz.OpenSubKey(subchave);
                        if (chave?.GetValue("InstallLocation") is string pastaInstalacao
                            && !string.IsNullOrWhiteSpace(pastaInstalacao))
                        {
                            string caminhoExe = Path.Combine(pastaInstalacao, "ElsEvo.exe");
                            if (File.Exists(caminhoExe))
                                return caminhoExe;
                        }
                    }
                    catch
                    {
                        // Chave inacessível ou inexistente nessa combinação — tenta a próxima.
                    }
                }
            }

            return null;
        }
    }
}
