using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace ElsEVO
{
    public partial class PreferenciasWindow : Window
    {
        private bool _carregando = true;

        private const string PlaceholderArgumentos = "argumentos | ex: 8f9slxa02nkp29ak1u26mqpcms";

        private bool _ajustandoPlaceholder;

        public PreferenciasWindow()
        {
            InitializeComponent();
            ThemeManager.AplicarTemaSalvo();
            CarregarConfiguracoes();
            AplicarIdioma();
            _carregando = false;

            SourceInitialized += (_, _) =>
                BarraTituloNativa.AplicarTema(this, !Properties.Settings.Default.TemaClaro);

            ConectarDeteccaoDeAlteracoes();

            TxtArgumentos.GotFocus += TxtArgumentos_GotFocus;
            TxtArgumentos.LostFocus += TxtArgumentos_LostFocus;
        }

        private void ConectarDeteccaoDeAlteracoes()
        {
            void Marcar(object? sender, EventArgs e)
            {
                if (!_carregando && !_ajustandoPlaceholder)
                    BtnAplicar.IsEnabled = true;
            }

            foreach (var chk in new[]
                     {
                         ChkNaoExecutarLauncher, ChkPularElsword, ChkBloquearLogs,
                         ChkMinimizarBandeja, ChkIniciarMinimizado, ChkIniciarComWindows,
                         ChkBuscarAtualizacoes, ChkBetaApenas
                     })
            {
                chk.Checked += Marcar;
                chk.Unchecked += Marcar;
            }

            RadioTemaClaro.Checked += Marcar;
            RadioTemaEscuro.Checked += Marcar;
            CmbIdioma.SelectionChanged += Marcar;
            TxtArgumentos.TextChanged += Marcar;

            BtnAplicar.IsEnabled = false;
        }

        private void AplicarIdioma()
        {
            Title = Idiomas.T("TituloConfiguracoes");
            AbaElsword.Header = Idiomas.T("AbaElsword");
            AbaInicializador.Header = Idiomas.T("AbaInicializador");
            BtnOk.Content = Idiomas.T("BotaoOk");
            BtnCancelar.Content = Idiomas.T("BotaoCancelar");
            BtnAplicar.Content = Idiomas.T("BotaoAplicar");

            GrpLocalizacaoJogo.Header = Idiomas.T("GrpLocalizacaoJogo");
            GrpOpcoesInicializacao.Header = Idiomas.T("GrpOpcoesInicializacao");
            ChkNaoExecutarLauncher.Content = Idiomas.T("ChkNaoExecutarLauncher");
            TxtRecomendadoCoreano.Text = Idiomas.T("TxtRecomendadoCoreano");
            ChkPularElsword.Content = Idiomas.T("ChkPularElsword");
            GrpSeguranca.Header = Idiomas.T("GrpSeguranca");
            ChkBloquearLogs.Content = Idiomas.T("ChkBloquearLogs");
            TxtAvisoLogs.Text = Idiomas.T("TxtAvisoLogs");

            GrpIdiomas.Header = Idiomas.T("GrpIdiomas");
            GrpTema.Header = Idiomas.T("GrpTema");
            RadioTemaClaro.Content = Idiomas.T("RadioClaro");
            RadioTemaEscuro.Content = Idiomas.T("RadioEscuro");
            GrpIconeBandeja.Header = Idiomas.T("GrpIconeBandeja");
            ChkMinimizarBandeja.Content = Idiomas.T("ChkMinimizarBandeja");
            ChkIniciarMinimizado.Content = Idiomas.T("ChkIniciarMinimizado");
            ChkIniciarComWindows.Content = Idiomas.T("ChkIniciarComWindows");
            GrpAtualizacoes.Header = Idiomas.T("GrpAtualizacoes");
            ChkBuscarAtualizacoes.Content = Idiomas.T("ChkBuscarAtualizacoes");
            ChkBetaApenas.Content = Idiomas.T("ChkBetaApenas");
            TxtAvisoBetaApenas.Text = Idiomas.T("TxtAvisoBetaApenas");
        }

        private void CarregarConfiguracoes()
        {
            var cfg = Properties.Settings.Default;

            bool temCaminhoReal = !string.IsNullOrWhiteSpace(cfg.ElswordDirectory);
            TxtLocalizacaoJogo.Text = temCaminhoReal
                ? Path.Combine(cfg.ElswordDirectory, "elsword.exe")
                : "ex: C:\\Elsword\\elsword.exe";
            AtualizarAparenciaPlaceholder(temCaminhoReal);

            ChkBloquearLogs.IsChecked = cfg.BlockLogs;
            ChkNaoExecutarLauncher.IsChecked = cfg.WebLoginNeeded;

            ChkPularElsword.IsChecked = cfg.SkipLauncher;
            TxtArgumentos.IsEnabled = cfg.SkipLauncher;

            _ajustandoPlaceholder = true;
            if (string.IsNullOrWhiteSpace(cfg.X2Args))
            {
                TxtArgumentos.Text = PlaceholderArgumentos;
                TxtArgumentos.Foreground = (System.Windows.Media.Brush)FindResource("CorTextoSecundario");
            }
            else
            {
                TxtArgumentos.Text = cfg.X2Args;
                TxtArgumentos.Foreground = (System.Windows.Media.Brush)FindResource("CorTextoPrimario");
            }
            _ajustandoPlaceholder = false;

            RadioTemaClaro.IsChecked = cfg.TemaClaro;
            RadioTemaEscuro.IsChecked = !cfg.TemaClaro;

            ChkBetaApenas.IsChecked = !cfg.IgnoreBetaReleases;
            ChkBuscarAtualizacoes.IsChecked = cfg.CheckForProgramUpdates;

            ChkMinimizarBandeja.IsChecked = cfg.MinimizarParaBandeja;
            ChkIniciarMinimizado.IsChecked = cfg.StartHidden;
            ChkIniciarComWindows.IsChecked = cfg.IniciarComWindows;

            CmbIdioma.SelectedIndex = cfg.Idioma switch
            {
                "en" => 1,
                "zh" => 2,
                _ => 0
            };
        }

        private void BtnProcurarJogo_Click(object sender, RoutedEventArgs e)
        {
            var dialogo = new OpenFileDialog
            {
                Title = "Selecione o elsword.exe",
                Filter = "elsword.exe|elsword.exe|Executáveis (*.exe)|*.exe",
                FileName = "elsword.exe"
            };

            if (dialogo.ShowDialog() == true)
            {
                TxtLocalizacaoJogo.Text = dialogo.FileName;
                AtualizarAparenciaPlaceholder(temCaminhoReal: true);
                if (!_carregando)
                    BtnAplicar.IsEnabled = true;
            }
        }

        private void ChkPularElsword_CheckedChanged(object sender, RoutedEventArgs e)
        {
            TxtArgumentos.IsEnabled = ChkPularElsword.IsChecked == true;
        }

        private void AtualizarAparenciaPlaceholder(bool temCaminhoReal)
        {
            TxtLocalizacaoJogo.Foreground = temCaminhoReal
                ? (System.Windows.Media.Brush)FindResource("CorTextoPrimario")
                : (System.Windows.Media.Brush)FindResource("CorTextoSecundario");
        }

        private void TxtArgumentos_GotFocus(object sender, RoutedEventArgs e)
        {
            if (TxtArgumentos.Text == PlaceholderArgumentos)
            {
                _ajustandoPlaceholder = true;
                TxtArgumentos.Text = string.Empty;
                TxtArgumentos.Foreground = (System.Windows.Media.Brush)FindResource("CorTextoPrimario");
                _ajustandoPlaceholder = false;
            }
        }

        private void TxtArgumentos_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtArgumentos.Text))
            {
                _ajustandoPlaceholder = true;
                TxtArgumentos.Text = PlaceholderArgumentos;
                TxtArgumentos.Foreground = (System.Windows.Media.Brush)FindResource("CorTextoSecundario");
                _ajustandoPlaceholder = false;
            }
        }

        private void RadioTema_Checked(object sender, RoutedEventArgs e)
        {
        }

        private void CmbIdioma_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void SalvarConfiguracoes()
        {
            var cfg = Properties.Settings.Default;

            string caminhoExe = TxtLocalizacaoJogo.Text;
            cfg.ElswordDirectory = File.Exists(caminhoExe)
                ? Path.GetDirectoryName(caminhoExe) ?? string.Empty
                : cfg.ElswordDirectory;

            cfg.BlockLogs = ChkBloquearLogs.IsChecked == true;
            cfg.WebLoginNeeded = ChkNaoExecutarLauncher.IsChecked == true;
            cfg.SkipLauncher = ChkPularElsword.IsChecked == true;
            cfg.X2Args = TxtArgumentos.Text == PlaceholderArgumentos ? string.Empty : TxtArgumentos.Text;
            cfg.TemaClaro = RadioTemaClaro.IsChecked == true;
            cfg.IgnoreBetaReleases = ChkBetaApenas.IsChecked != true;
            cfg.CheckForProgramUpdates = ChkBuscarAtualizacoes.IsChecked == true;

            cfg.MinimizarParaBandeja = ChkMinimizarBandeja.IsChecked == true;
            cfg.StartHidden = ChkIniciarMinimizado.IsChecked == true;

            bool iniciarComWindows = ChkIniciarComWindows.IsChecked == true;
            cfg.IniciarComWindows = iniciarComWindows;
            InicializacaoComWindows.Aplicar(iniciarComWindows);

            string codigoIdioma = CmbIdioma.SelectedIndex switch
            {
                1 => "en",
                2 => "zh",
                _ => "pt"
            };

            cfg.Save();

            ThemeManager.AplicarTema(cfg.TemaClaro);
            BarraTituloNativa.AplicarTema(this, !cfg.TemaClaro);
            Idiomas.DefinirIdioma(codigoIdioma);
            AplicarIdioma();

            BtnAplicar.IsEnabled = false;
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SalvarConfiguracoes();
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Não foi possível salvar as configurações:\n{ex.Message}",
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnAplicar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SalvarConfiguracoes();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Não foi possível salvar as configurações:\n{ex.Message}",
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
