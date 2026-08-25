using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ElsEvo
{
    public partial class DublagensWindow : Window
    {
        private CancellationTokenSource? _cancelamentoAtual;
        private readonly DispatcherTimer _animacao;
        private List<DublagemDisponivel> _dublagens = new();
        private int _pontos;

        public DublagensWindow()
        {
            InitializeComponent();
            SourceInitialized += (_, _) =>
                BarraTituloNativa.AplicarTema(this, !Properties.Settings.Default.TemaClaro);

            _animacao = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
            _animacao.Tick += (_, _) =>
            {
                _pontos = (_pontos + 1) % 4;
                TxtStatus.Text = "Carregando dublagens" + new string('.', _pontos);
            };
            Loaded += async (_, _) => await CarregarDublagensAsync();
            Closed += (_, _) => _cancelamentoAtual?.Cancel();
        }

        private async Task CarregarDublagensAsync()
        {
            using var cancelamento = new CancellationTokenSource();
            _cancelamentoAtual = cancelamento;
            _animacao.Start();
            try
            {
                _dublagens = await DublagensService.ListarAsync(cancelamento.Token);
                ListaDublagens.ItemsSource = _dublagens;
                TxtStatus.Text = _dublagens.Count == 0
                    ? "Nenhuma dublagem disponível."
                    : $"{_dublagens.Count} dublagem(ns) disponível(is).";
            }
            catch (OperationCanceledException)
            {
                TxtStatus.Text = "Operação cancelada.";
            }
            catch (Exception ex)
            {
                RegistroLog.Erro("Falha ao carregar catálogo de dublagens", ex);
                TxtStatus.Text = "Não foi possível carregar as dublagens.";
                MessageBox.Show("Não foi possível carregar o catálogo de dublagens.\n\n" + ex.Message,
                    "Baixar dublagens", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                _animacao.Stop();
                if (ReferenceEquals(_cancelamentoAtual, cancelamento))
                    _cancelamentoAtual = null;
            }
        }

        private void ListaDublagens_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            BtnBaixar.IsEnabled = ListaDublagens.SelectedItem is DublagemDisponivel;
        }

        private async void BtnBaixar_Click(object sender, RoutedEventArgs e)
        {
            if (ListaDublagens.SelectedItem is not DublagemDisponivel dublagem)
                return;

            BtnBaixar.IsEnabled = false;
            BtnCancelar.IsEnabled = true;
            _animacao.Start();
            using var cancelamento = new CancellationTokenSource();
            _cancelamentoAtual = cancelamento;
            RegistroLog.Registrar("Download de dublagem iniciado", dublagem.Id);

            try
            {
                var progresso = new Progress<int>(valor => BarraProgresso.Value = valor);
                var status = new Progress<string>(texto => TxtStatus.Text = texto + (_pontos++ % 4 == 0 ? "." : string.Empty));
                await DublagensService.BaixarEInstalarAsync(dublagem, progresso, status, cancelamento.Token);
                RegistroLog.Registrar("Download de dublagem concluído", dublagem.Id);
                MessageBox.Show("A dublagem foi baixada e instalada nos seus mods.",
                    "Baixar dublagens", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (OperationCanceledException)
            {
                RegistroLog.Registrar("Download de dublagem cancelado", dublagem.Id);
                TxtStatus.Text = "Download cancelado.";
            }
            catch (Exception ex)
            {
                RegistroLog.Erro($"Falha ao instalar dublagem {dublagem.Id}", ex);
                TxtStatus.Text = "Falha no download.";
                MessageBox.Show("Não foi possível instalar a dublagem.\n\n" + ex.Message,
                    "Baixar dublagens", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _animacao.Stop();
                BtnBaixar.IsEnabled = ListaDublagens.SelectedItem is DublagemDisponivel;
                BtnCancelar.IsEnabled = false;
                if (ReferenceEquals(_cancelamentoAtual, cancelamento))
                    _cancelamentoAtual = null;
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            if (_cancelamentoAtual != null && !_cancelamentoAtual.IsCancellationRequested)
            {
                RegistroLog.Registrar("Cancelamento do download de dublagem solicitado");
                _cancelamentoAtual.Cancel();
                BtnCancelar.IsEnabled = false;
                TxtStatus.Text = "Cancelando...";
            }
        }
    }
}
