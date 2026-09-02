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
        private bool _podeCancelar;
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
            BtnCancelar.Content = Idiomas.T("Cancelar");
            StatusBadge.Text = _modsLigados ? Idiomas.T("Ligado") : Idiomas.T("Desligado");
            AtualizarTextoBotaoJogar();
        }

        private void AtualizarListaDeModsAtivos()
        {
            var ativos = GerenciadorDeMods.Carregar();
            ListaModsAtivos.Items.Clear();

            var porPack = ativos.GroupBy(m => m.NomeDoPack);
            foreach (var grupo in porPack)
            {
                int quantidade = grupo.Count();
                int ausentes = grupo.Count(m => !File.Exists(m.CaminhoCompleto));
                string nome = quantidade == 1 ? grupo.Key : $"{grupo.Key}  ({quantidade} arquivos)";
                if (ausentes > 0)
                    nome += $"  -  {string.Format(Idiomas.T("ArquivosAusentes"), ausentes)}";

                var item = new ListBoxItem
                {
                    Padding = new Thickness(6),
                    Content = nome,
                };
                if (ausentes > 0)
                    item.Foreground = Brushes.Orange;
                ListaModsAtivos.Items.Add(item);
            }

            bool temMods = ativos.Count > 0;
            ListaModsAtivos.Visibility = temMods ? Visibility.Visible : Visibility.Collapsed;
            TxtListaVazia.Visibility = temMods ? Visibility.Collapsed : Visibility.Visible;
        }

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

        private void BtnToggleLigado_Click(object sender, RoutedEventArgs e)
        {
            _modsLigados = !_modsLigados;
            RegistroLog.Registrar("Mods alternados", _modsLigados ? "Ligados" : "Desligados");
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

        private void AtualizarTextoBotaoJogar()
        {
            TxtStatusJogar.Text = _modsLigados ? Idiomas.T("BtnAplicarJogar") : Idiomas.T("BtnExecutarLauncher");
        }

        private void MenuReiniciar_Click(object sender, RoutedEventArgs e)
        {
            RegistroLog.Registrar("Reinício solicitado");
            string caminhoExeAtual = Process.GetCurrentProcess().MainModule?.FileName
                                      ?? Environment.ProcessPath
                                      ?? string.Empty;

            if (!string.IsNullOrEmpty(caminhoExeAtual))
                Process.Start(caminhoExeAtual);

            Application.Current.Shutdown();
        }

        private void MenuLimparCache_Click(object sender, RoutedEventArgs e)
        {
            RegistroLog.Registrar("Limpeza de cache solicitada");
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

        private void MenuLimparConfiguracoes_Click(object sender, RoutedEventArgs e)
        {
            RegistroLog.Registrar("Limpeza de configurações solicitada");
            bool confirmou = JanelaConfirmacao.Confirmar(this,
                "Limpar configurações",
                "Isso vai restaurar todas as configurações do ElsEvo para o padrão. Continuar?",
                TipoMensagem.Aviso);

            if (!confirmou)
                return;

            Properties.Settings.Default.Reset();
            Properties.Settings.Default.Save();
            Paths.InvalidarCache();

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
            RegistroLog.Registrar("Exclusão de todos os mods solicitada");
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

        private void MenuConfiguracoes_Click(object sender, RoutedEventArgs e)
        {
            RegistroLog.Registrar("Janela de configurações aberta");
            var janela = new PreferenciasWindow { Owner = this };
            janela.ShowDialog();

            BadgeBeta.Visibility = Properties.Settings.Default.IsBetaRelease ? Visibility.Visible : Visibility.Collapsed;
            AtualizarVisualToggle();
            AplicarIdioma();
        }

        private void MenuSobre_Click(object sender, RoutedEventArgs e)
        {
            RegistroLog.Registrar("Janela Sobre aberta");
            var janela = new SobreWindow { Owner = this };
            janela.ShowDialog();
        }

        private void BtnGerenciarMods_Click(object sender, RoutedEventArgs e)
        {
            RegistroLog.Registrar("Janela Gerenciar Mods aberta");
            var janela = new GerenciarModsWindow { Owner = this };
            janela.ShowDialog();

            AtualizarListaDeModsAtivos();
        }

        private void BtnBaixarDublagens_Click(object sender, RoutedEventArgs e)
        {
            RegistroLog.Registrar("Janela de download de dublagens aberta");
            var janela = new DublagensWindow { Owner = this };
            janela.ShowDialog();
            AtualizarListaDeModsAtivos();
        }

        private async void BtnJogar_Click(object sender, RoutedEventArgs e)
        {
            RegistroLog.Registrar("Aplicar e Jogar solicitado", _modsLigados ? "Mods ligados" : "Mods desligados");
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
                var modsAusentes = ativos
                    .Where(m => !File.Exists(m.CaminhoCompleto))
                    .ToList();

                if (modsAusentes.Count > 0)
                {
                    string nomesAusentes = string.Join("\n", modsAusentes.Select(m => $"• {m.NomeDoPack}: {m.Arquivo}"));
                    JanelaConfirmacao.Mostrar(this,
                        Idiomas.T("ModsAusentesTitulo"),
                        string.Format(Idiomas.T("ModsAusentesMensagem"), nomesAusentes),
                        TipoMensagem.Aviso);
                }

                listaDePatches = ativos
                    .Where(m => File.Exists(m.CaminhoCompleto))
                    .Select(m => new PatchInfo(m))
                    .ToList();
            }

            BtnJogar.IsEnabled = false;
            TxtStatusJogar.Text = "Aguardando o launcher...";

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
                TxtStatusJogar.Text = estado switch
                {
                    EstadoPatch.PreparandoArquivos => "Preparando arquivos...",
                    EstadoPatch.AguardandoElswordAbrir => "Aguardando o launcher fechar...",
                    EstadoPatch.FazendoBackup => "Fazendo backup...",
                    EstadoPatch.Aplicando => "Aplicando mods...",
                    EstadoPatch.AguardandoElswordFechar => "Mods ativos — divirta-se! 🎮",
                    EstadoPatch.RestaurandoBackup => "Restaurando backup...",
                    _ => "Concluído"
                };
                _podeCancelar = estado is EstadoPatch.PreparandoArquivos
                    or EstadoPatch.AguardandoElswordAbrir;
                if (!_podeCancelar)
                    BtnCancelar.Visibility = Visibility.Collapsed;
            });

            _cancelamentoAtual = new CancellationTokenSource();
            _podeCancelar = true;

            try
            {
                await PatcherService.ExecutarFluxoPatchAsync(
                    listaDePatches, progresso, statusProgresso, _cancelamentoAtual.Token);
            }
            catch (OperationCanceledException)
            {
                RegistroLog.Registrar("Patch cancelado");
                JanelaConfirmacao.Mostrar(this,
                    Idiomas.T("Cancelar"),
                    Idiomas.T("OperacaoCancelada"),
                    TipoMensagem.Informacao);
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
                BtnCancelar.IsEnabled = true;
                BtnCancelar.Visibility = Visibility.Collapsed;
                _podeCancelar = false;
                _cancelamentoAtual = null;
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            if (_cancelamentoAtual == null)
                return;

            BtnCancelar.IsEnabled = false;
            RegistroLog.Registrar("Cancelamento do patch solicitado");
            TxtStatusJogar.Text = "Cancelando...";
            _cancelamentoAtual.Cancel();
        }

        private void StatusPatchContainer_MouseEnter(object sender, MouseEventArgs e)
        {
            if (_cancelamentoAtual != null && _podeCancelar)
                BtnCancelar.Visibility = Visibility.Visible;
        }

        private void StatusPatchContainer_MouseLeave(object sender, MouseEventArgs e)
        {
            if (_cancelamentoAtual == null || !_podeCancelar)
                BtnCancelar.Visibility = Visibility.Collapsed;
        }

        private async Task VerificarAtualizacaoAsync()
        {
            if (!Properties.Settings.Default.CheckForProgramUpdates)
                return;

            var atualizacao = await AtualizacaoService.VerificarAsync();
            if (atualizacao == null)
                return;

            var janela = new AtualizacaoWindow(atualizacao) { Owner = this };
            bool? resposta = janela.ShowDialog();

            if (resposta != true)
                return;

            await BaixarEInstalarAtualizacaoAsync(atualizacao);
        }

        private async Task BaixarEInstalarAtualizacaoAsync(AtualizacaoDisponivel atualizacao)
        {
            string caminhoInstalador = Path.Combine(Path.GetTempPath(), "ElsEvo-Setup.exe");

            BtnJogar.IsEnabled = false;
            BtnGerenciarMods.IsEnabled = false;
            BtnBaixarDublagens.IsEnabled = false;
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
                    var processoInstalador = Process.Start(new ProcessStartInfo
                    {
                        FileName = caminhoInstalador,
                        Arguments = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-",
                        UseShellExecute = true
                    });

                    if (processoInstalador == null)
                        throw new InvalidOperationException("Não foi possível iniciar o processo do instalador.");

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
                    BtnBaixarDublagens.IsEnabled = true;
                }
            }
        }

        private void ReabrirAppAtualizadoEFechar()
        {
            string? caminhoRegistro = ObterCaminhoExeInstalado();
            string? caminhoProcessoAtual = Process.GetCurrentProcess().MainModule?.FileName;
            string? caminhoExeNovo = caminhoRegistro ?? caminhoProcessoAtual;

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
                    }
                }
            }

            return null;
        }
    }
}
