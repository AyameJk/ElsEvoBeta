using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ElsEVO
{
    public partial class SobreWindow : Window
    {
        public SobreWindow()
        {
            InitializeComponent();
            ThemeManager.AplicarTemaSalvo();

            SourceInitialized += (_, _) =>
                BarraTituloNativa.AplicarTema(this, !Properties.Settings.Default.TemaClaro);

            ThemeManager.TemaMudou += AoTemaMudar;
            Closed += (_, _) => ThemeManager.TemaMudou -= AoTemaMudar;

            AplicarIdioma();

            bool isBeta = Properties.Settings.Default.IsBetaRelease;
            BadgeBeta.Visibility = isBeta ? Visibility.Visible : Visibility.Collapsed;

            if (isBeta)
            {
                TxtVersaoBeta.Text = $"Versão Beta: {AppVersion.VersaoParaAtualizacao}";
                TxtVersaoBeta.Visibility = Visibility.Visible;
            }

            AtualizarCorBadgeBeta(Properties.Settings.Default.TemaClaro);
        }

        private void AoTemaMudar(bool temaClaro)
        {
            BarraTituloNativa.AplicarTema(this, !temaClaro);
            AtualizarCorBadgeBeta(temaClaro);
        }

        private void AtualizarCorBadgeBeta(bool temaClaro)
        {
            var bc = new BrushConverter();
            var texto = (TextBlock)BadgeBeta.Child;

            if (temaClaro)
            {
                BadgeBeta.Background = (Brush)bc.ConvertFrom("#D32F2F")!;
                BadgeBeta.BorderThickness = new Thickness(0);
                texto.Foreground = Brushes.White;
            }
            else
            {
                BadgeBeta.Background = (Brush)bc.ConvertFrom("#3D2F1E")!;
                BadgeBeta.BorderBrush = (Brush)bc.ConvertFrom("#B8860B")!;
                BadgeBeta.BorderThickness = new Thickness(1);
                texto.Foreground = (Brush)bc.ConvertFrom("#E0B060")!;
            }
        }

        private void AplicarIdioma()
        {
            Title = Idiomas.T("TituloSobre");
            TxtVersao.Text = AppVersion.Numero;
            TxtDescricao.Text = Idiomas.T("SobreDescricao");
            TxtRotuloAutor.Text = Idiomas.T("SobreAutor");
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e) => Close();
    }
}
