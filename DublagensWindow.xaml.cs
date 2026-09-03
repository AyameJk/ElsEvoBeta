using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ElsEvo
{
    public sealed class DublagemItemVM : INotifyPropertyChanged
    {
        private bool _instalado;
        private bool _aguardando;
        private bool _baixando;
        private bool _pausado;
        private bool _selecionado;

        public DublagemDisponivel Dublagem { get; }
        public string Nome => Dublagem.Nome;
        public string Descricao => Dublagem.Descricao;
        public string TextoBaixando => Idiomas.T("DublagemBaixando");
        public string TextoNaFila => Idiomas.T("DublagemNaFila");
        public string TextoInstalado => Idiomas.T("DublagemInstalado");
        public string TextoPausado => Idiomas.T("DublagemPausado");

        public string? TamanhoFormatado => Dublagem.TamanhoBytes is long bytes && bytes > 0
            ? DublagensService.FormatarTamanho(bytes)
            : null;

        public Visibility VisibilidadeTamanho => TamanhoFormatado != null ? Visibility.Visible : Visibility.Collapsed;

        public bool Selecionado
        {
            get => _selecionado;
            set
            {
                if (_selecionado == value)
                    return;

                _selecionado = value;
                OnPropertyChanged(nameof(Selecionado));
            }
        }

        public bool Instalado
        {
            get => _instalado;
            set
            {
                if (_instalado == value)
                    return;

                _instalado = value;
                OnPropertyChanged(nameof(Instalado));
                OnPropertyChanged(nameof(VisibilidadeInstalado));
            }
        }

        public bool Aguardando
        {
            get => _aguardando;
            set
            {
                if (_aguardando == value)
                    return;

                _aguardando = value;
                OnPropertyChanged(nameof(Aguardando));
                OnPropertyChanged(nameof(VisibilidadeAguardando));
                OnPropertyChanged(nameof(VisibilidadeInstalado));
            }
        }

        public bool Baixando
        {
            get => _baixando;
            set
            {
                if (_baixando == value)
                    return;

                _baixando = value;
                OnPropertyChanged(nameof(Baixando));
                OnPropertyChanged(nameof(VisibilidadeBaixando));
                OnPropertyChanged(nameof(VisibilidadeInstalado));
            }
        }

        public bool Pausado
        {
            get => _pausado;
            set
            {
                if (_pausado == value)
                    return;

                _pausado = value;
                OnPropertyChanged(nameof(Pausado));
                OnPropertyChanged(nameof(VisibilidadePausado));
                OnPropertyChanged(nameof(VisibilidadeInstalado));
            }
        }

        public Visibility VisibilidadeInstalado => (Instalado && !Aguardando && !Baixando && !Pausado) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility VisibilidadeAguardando => Aguardando ? Visibility.Visible : Visibility.Collapsed;
        public Visibility VisibilidadeBaixando => Baixando ? Visibility.Visible : Visibility.Collapsed;
        public Visibility VisibilidadePausado => Pausado ? Visibility.Visible : Visibility.Collapsed;

        public DublagemItemVM(DublagemDisponivel dublagem, bool instalado)
        {
            Dublagem = dublagem;
            _instalado = instalado;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string nome) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nome));
    }

    public partial class DublagensWindow : Window
    {
        private CancellationTokenSource? _cancelamentoAtual;
        private CancellationTokenSource? _pausaAtual;
        private readonly DispatcherTimer _animacao;
        private List<DublagemItemVM> _dublagens = new();
        private int _pontos;

        public DublagensWindow()
        {
            InitializeComponent();
            AplicarIdioma();
            SourceInitialized += (_, _) =>
                BarraTituloNativa.AplicarTema(this, !Properties.Settings.Default.TemaClaro);

            _animacao = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
            _animacao.Tick += (_, _) =>
            {
                _pontos = (_pontos + 1) % 4;
                TxtStatus.Text = Idiomas.T("DublagemCarregando") + new string('.', _pontos);
            };
            Loaded += async (_, _) => await CarregarDublagensAsync();
            Closed += (_, _) => _cancelamentoAtual?.Cancel();
        }

        private void AplicarIdioma()
        {
            TxtTituloDublagens.Text = Idiomas.T("DublagensDisponiveis");
            TxtInstrucaoDublagens.Text = Idiomas.T("DublagensInstrucao");
            TxtFilaTotal.Text = Idiomas.T("DublagemFilaTotal");
            TxtArquivoAtual.Text = Idiomas.T("DublagemArquivoAtual");
            BtnPausar.Content = Idiomas.T("DublagemPausar");
            BtnCancelar.Content = Idiomas.T("BotaoCancelar");
            BtnBaixar.Content = Idiomas.T("DublagemBaixar");
        }

        private async Task CarregarDublagensAsync()
        {
            using var cancelamento = new CancellationTokenSource();
            _cancelamentoAtual = cancelamento;
            _animacao.Start();
            try
            {
                var brutas = await DublagensService.ListarAsync(cancelamento.Token);
                _dublagens = brutas
                    .Select(d => new DublagemItemVM(d, PackJaInstalado(d)))
                    .ToList();

                foreach (var item in _dublagens)
                    item.PropertyChanged += Item_PropertyChanged;

                ListaDublagens.ItemsSource = _dublagens;
                TxtStatus.Text = _dublagens.Count == 0
                    ? Idiomas.T("DublagemNenhuma")
                    : string.Format(Idiomas.T("DublagemQuantidade"), _dublagens.Count);

                AtualizarBotaoBaixar();
            }
            catch (OperationCanceledException)
            {
                TxtStatus.Text = Idiomas.T("OperacaoCancelada");
            }
            catch (Exception ex)
            {
                RegistroLog.Erro("Falha ao carregar catálogo de dublagens", ex);
                TxtStatus.Text = Idiomas.T("DublagemFalhaCatalogo");
                JanelaConfirmacao.Mostrar(this,
                    "Baixar dublagens",
                    "Não foi possível carregar o catálogo de dublagens.\n\n" + ex.Message,
                    TipoMensagem.Aviso);
            }
            finally
            {
                _animacao.Stop();
                if (ReferenceEquals(_cancelamentoAtual, cancelamento))
                    _cancelamentoAtual = null;
            }
        }

        private static string SanitizarNome(string nome)
        {
            string limpo = new(nome.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_').ToArray());
            return string.IsNullOrWhiteSpace(limpo) ? "Dublagem" : limpo;
        }

        private static bool PackJaInstalado(DublagemDisponivel dublagem)
        {
            try
            {
                string pastaPack = Path.Combine(Paths.Main.Packs, SanitizarNome(dublagem.Id));
                return Directory.Exists(pastaPack) && Directory.EnumerateFileSystemEntries(pastaPack).Any();
            }
            catch
            {
                return false;
            }
        }

        private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DublagemItemVM.Selecionado))
                AtualizarBotaoBaixar();
        }

        private void AtualizarBotaoBaixar()
        {
            int quantidade = _dublagens.Count(d => d.Selecionado);
            BtnBaixar.IsEnabled = quantidade > 0;
            BtnBaixar.Content = quantidade > 1
                ? string.Format(Idiomas.T("DublagemBaixarSelecionadas"), quantidade)
                : Idiomas.T("DublagemBaixar");
        }

        private void AtualizarBarraFila(int concluidos, int total, int percentualItemAtual = 0)
        {
            if (total == 0)
            {
                BarraProgressoFila.Value = 0;
                TxtPercentFila.Text = "0/0 (0%)";
                return;
            }

            double progressoFracionario = concluidos + (percentualItemAtual / 100.0);
            int percentual = (int)(progressoFracionario * 100 / total);
            percentual = Math.Clamp(percentual, 0, 100);

            BarraProgressoFila.Value = percentual;
            TxtPercentFila.Text = $"{concluidos}/{total} ({percentual}%)";
        }

        private static string FormatarTextoProgresso(ProgressoDownload info)
        {
            string recebido = DublagensService.FormatarTamanho(info.BytesRecebidos);

            if (info.BytesTotal is long total && total > 0)
            {
                string totalFormatado = DublagensService.FormatarTamanho(total);
                string velocidade = DublagensService.FormatarTamanho((long)(info.VelocidadeKBps * 1024));
                return $"{info.Percentual}% — {recebido} / {totalFormatado} — {velocidade}/s";
            }

            return $"{info.Percentual}% — {recebido}";
        }

        private async void BtnBaixar_Click(object sender, RoutedEventArgs e)
        {
            var fila = _dublagens.Where(d => d.Selecionado).ToList();
            if (fila.Count == 0)
                return;

            var jaInstalados = fila.Where(i => i.Instalado).ToList();
            if (jaInstalados.Count > 0)
            {
                string nomes = string.Join(", ", jaInstalados.Select(i => i.Nome));
                bool confirmou = JanelaConfirmacao.Confirmar(this,
                    "Dublagem já instalada",
                    jaInstalados.Count == fila.Count
                        ? $"\"{nomes}\" já foi baixada anteriormente. Deseja baixar e substituir os arquivos mesmo assim?"
                        : $"As seguintes dublagens já estão instaladas: {nomes}. Deseja baixá-las novamente também?",
                    TipoMensagem.Aviso);

                if (!confirmou)
                    fila = fila.Except(jaInstalados).ToList();
            }

            if (fila.Count == 0)
                return;

            BtnBaixar.IsEnabled = false;
            BtnCancelar.IsEnabled = true;
            BtnPausar.Visibility = Visibility.Visible;
            BtnPausar.IsEnabled = false;
            BtnPausar.Content = Idiomas.T("DublagemPausar");

            ListaDublagens.IsHitTestVisible = false;

            foreach (var item in fila)
            {
                if (!item.Pausado)
                    item.Aguardando = true;
            }

            int total = fila.Count;
            int concluidos = fila.Count(i => i.Instalado);
            AtualizarBarraFila(concluidos, total);
            BarraProgresso.Value = 0;
            TxtPercentAtual.Text = "0%";

            using var cancelamento = new CancellationTokenSource();
            _cancelamentoAtual = cancelamento;

            bool cancelado = false;
            bool pausado = false;

            foreach (var item in fila)
            {
                if (cancelamento.IsCancellationRequested)
                    break;

                var dublagem = item.Dublagem;
                RegistroLog.Registrar("Download de dublagem iniciado", dublagem.Id);
                BarraProgresso.Value = 0;
                TxtPercentAtual.Text = "0%";
                TxtStatus.Text = $"Baixando {dublagem.Nome}...";
                AtualizarBarraFila(concluidos, total, 0);

                item.Aguardando = false;
                item.Pausado = false;
                item.Baixando = true;
                BtnPausar.IsEnabled = true;

                using var pausaItem = new CancellationTokenSource();
                _pausaAtual = pausaItem;
                using var linkado = CancellationTokenSource.CreateLinkedTokenSource(cancelamento.Token, pausaItem.Token);

                try
                {
                    var progresso = new Progress<ProgressoDownload>(info =>
                    {
                        BarraProgresso.Value = info.Percentual;
                        TxtPercentAtual.Text = FormatarTextoProgresso(info);
                        AtualizarBarraFila(concluidos, total, info.Percentual);
                    });
                    var status = new Progress<string>(texto => TxtStatus.Text = texto);

                    await DublagensService.BaixarEInstalarAsync(dublagem, progresso, status, linkado.Token, pausaItem.Token);

                    RegistroLog.Registrar("Download de dublagem concluído", dublagem.Id);
                    item.Instalado = true;
                    item.Selecionado = false;
                    concluidos++;
                    AtualizarBarraFila(concluidos, total);
                }
                catch (OperationCanceledException) when (pausaItem.IsCancellationRequested && !cancelamento.IsCancellationRequested)
                {
                    RegistroLog.Registrar("Download de dublagem pausado", dublagem.Id);
                    item.Pausado = true;
                    pausado = true;
                }
                catch (OperationCanceledException)
                {
                    RegistroLog.Registrar("Download de dublagem cancelado", dublagem.Id);
                    cancelado = true;
                }
                catch (Exception ex)
                {
                    RegistroLog.Erro($"Falha ao instalar dublagem {dublagem.Id}", ex);
                    JanelaConfirmacao.Mostrar(this,
                        "Baixar dublagens",
                        $"Não foi possível instalar \"{dublagem.Nome}\".\n\n{ex.Message}",
                        TipoMensagem.Erro);
                }
                finally
                {
                    item.Baixando = false;
                    if (ReferenceEquals(_pausaAtual, pausaItem))
                        _pausaAtual = null;
                }

                if (pausado || cancelado)
                    break;
            }

            BarraProgresso.Value = 0;
            TxtPercentAtual.Text = "0%";
            BtnPausar.Visibility = Visibility.Collapsed;

            if (cancelado)
            {
                foreach (var item in fila)
                {
                    item.Aguardando = false;
                    item.Baixando = false;
                    item.Pausado = false;
                }

                TxtStatus.Text = Idiomas.T("DublagemCancelar");
            }
            else if (pausado)
            {
                foreach (var item in fila)
                {
                    if (!item.Instalado)
                        item.Aguardando = false;
                }

                TxtStatus.Text = Idiomas.T("DublagemPausadaStatus");
            }
            else
            {
                TxtStatus.Text = concluidos == total
                    ? $"{concluidos} dublagem(ns) instalada(s) com sucesso."
                    : $"{concluidos}/{total} dublagem(ns) instalada(s).";

                if (concluidos > 0)
                {
                    JanelaConfirmacao.Mostrar(this,
                        "Baixar dublagens",
                        concluidos == 1
                            ? "A dublagem foi baixada e instalada nos seus mods."
                            : $"{concluidos} dublagens foram baixadas e instaladas nos seus mods.",
                        TipoMensagem.Sucesso);
                }
            }

            ListaDublagens.IsHitTestVisible = true;
            BtnCancelar.IsEnabled = false;
            AtualizarBotaoBaixar();

            if (ReferenceEquals(_cancelamentoAtual, cancelamento))
                _cancelamentoAtual = null;
        }

        private void BtnPausar_Click(object sender, RoutedEventArgs e)
        {
            if (_pausaAtual != null && !_pausaAtual.IsCancellationRequested)
            {
                RegistroLog.Registrar("Pausa do download de dublagem solicitada");
                _pausaAtual.Cancel();
                BtnPausar.IsEnabled = false;
                TxtStatus.Text = Idiomas.T("DublagemPausando");
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            if (_cancelamentoAtual != null && !_cancelamentoAtual.IsCancellationRequested)
            {
                RegistroLog.Registrar("Cancelamento do download de dublagem solicitado");
                _cancelamentoAtual.Cancel();
                BtnCancelar.IsEnabled = false;
                BtnPausar.IsEnabled = false;
                TxtStatus.Text = Idiomas.T("DublagemCancelando");
            }
        }
    }
}
