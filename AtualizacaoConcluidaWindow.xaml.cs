using System.Windows;

namespace ElsEvo
{
    public partial class AtualizacaoConcluidaWindow : Window
    {
        public AtualizacaoConcluidaWindow()
        {
            InitializeComponent();
            ThemeManager.AplicarTemaSalvo();

            SourceInitialized += (_, _) =>
                BarraTituloNativa.AplicarTema(this, !Properties.Settings.Default.TemaClaro);

            TxtTitulo.Text = Idiomas.T("AtualizacaoConcluidaTitulo");
            TxtDetalhe.Text = string.Format(Idiomas.T("AtualizacaoConcluidaDetalhe"), AppVersion.VersaoParaAtualizacao);
            BtnOk.Content = Idiomas.T("BotaoOk");
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e) => Close();
    }
}
