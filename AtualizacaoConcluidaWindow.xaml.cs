using System.Windows;

namespace ElsEvo
{
    /// <summary>
    /// Janela pequena mostrada quando o ElsEvo detecta que acabou de reabrir sozinho
    /// depois de um update (ver argumento "--atualizado" passado pelo
    /// MainWindow.ReabrirAppAtualizadoEFechar). Segue o mesmo padrão visual das outras
    /// janelas temáticas (AtualizacaoWindow, SobreWindow) — ThemeManager + BarraTituloNativa.
    /// </summary>
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
