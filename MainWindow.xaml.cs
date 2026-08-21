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

        private void MenuLimparCache_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string pastaCache = Paths.Main.Cache;
                foreach (var arquivo in Directory.GetFiles(pastaCache))
                    File.Delete(arquivo);

                MessageBox.Show("Cache de arquivos limpo com sucesso.", "ElsEvo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Não foi possível limpar o cache:\n{ex.Message}", "ElsEvo",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuLimparConfiguracoes_Click(object sender, RoutedEventArgs e)
        {
            var resposta = MessageBox.Show(
                "Isso vai restaurar todas as configurações do ElsEvo para o padrão. Continuar?",
                "Limpar configurações",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (resposta != MessageBoxResult.Yes)
                return;

            Properties.Settings.Default.Reset();
            Properties.Settings.Default.Save();

            ThemeManager.AplicarTemaSalvo();
            InicializacaoComWindows.Aplicar(Properties.Settings.Default.IniciarComWindows);
            AplicarIdioma();

            MessageBox.Show("Configurações restauradas para o padrão.", "ElsEvo",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MenuExcluirMods_Click(object sender, RoutedEventArgs e)
        {
            var resposta = MessageBox.Show(
                "Isso vai excluir TODOS os packs de mods importados. Essa ação não pode ser desfeita. Continuar?",
                "Excluir todos os mods",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (resposta != MessageBoxResult.Yes)
                return;

            try
            {
                string pastaPacks = Paths.Main.Packs;
                if (Directory.Exists(pastaPacks))
                    Directory.Delete(pastaPacks, recursive: true);
                Directory.CreateDirectory(pastaPacks);

                GerenciadorDeMods.Salvar(new List<ModAtivo>());
                AtualizarListaDeModsAtivos();

                MessageBox.Show("Todos os mods foram excluídos.", "ElsEvo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Não foi possível excluir os mods:\n{ex.Message}", "ElsEvo",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(
                    "A pasta do Elsword configurada não é válida (precisa ter \"elsword.exe\" e a pasta \"data\").\n" +
                    "Configure em Configurações → Elsword → Localização do jogo.",
                    "ElsEvo", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                MessageBox.Show($"Ocorreu um erro durante o patch:\n{ex.Message}",
                    "ElsEvo", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    MessageBox.Show(
                        $"Não foi possível baixar a atualização automaticamente:\n{ex.Message}\n\n" +
                        "O ElsEvo vai continuar funcionando normalmente na versão atual. Você pode " +
                        "tentar de novo mais tarde, ou baixar manualmente pela página de Releases no GitHub.",
                        "Falha ao atualizar", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                    MessageBox.Show(
                        $"O instalador foi baixado, mas não foi possível executá-lo automaticamente:\n{ex.Message}\n\n" +
                        $"Você pode rodar ele manualmente em:\n{caminhoInstalador}",
                        "Falha ao iniciar instalador", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                timerPontinhos.Stop();

                // Código de saída do Inno Setup: 0 = sucesso. Qualquer outro valor indica que
                // algo deu errado (ex.: 5 = falha na instalação, 6 = cancelado pelo Restart
                // Manager) — como não tem assistente visível, o usuário não veria isso sozinho,
                // então mostramos um aviso explícito em vez de simplesmente reabrir o app.
                if (codigoSaida != 0)
                {
                    MessageBox.Show(
                        $"O instalador terminou com um erro (código {codigoSaida}) e a atualização pode não " +
                        "ter sido concluída corretamente.\n\n" +
                        "O ElsEvo vai continuar/reabrir normalmente. Se algo parecer errado, tente " +
                        "baixar e instalar manualmente pela página de Releases no GitHub.",
                        "Atenção — instalação da atualização", MessageBoxButton.OK, MessageBoxImage.Warning);
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
        /// onde ficou instalado — não assume que é a mesma pasta do processo atual (o
        /// usuário pode ter mudado o destino da instalação). Se não achar no registro por
        /// qualquer motivo, cai pro caminho do processo atual como último recurso.
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
