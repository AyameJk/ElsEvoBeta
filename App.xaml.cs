using System.Threading;
using System.Windows;
using ElsEvo.Properties;

namespace ElsEvo
{
    public partial class App : Application
    {
        private static Mutex? _mutexPrincipal;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            RegistroLog.Registrar("Aplicativo iniciado");

            _mutexPrincipal = new Mutex(initiallyOwned: true, name: "ElsEvo_MutexPrincipal");

            ThemeManager.AplicarTemaSalvo();
            InicializacaoComWindows.Aplicar(Settings.Default.IniciarComWindows);

            var janelaPrincipal = new MainWindow();
            MainWindow = janelaPrincipal;

            if (Settings.Default.StartHidden)
            {
                janelaPrincipal.WindowState = WindowState.Minimized;
                janelaPrincipal.Show();
                janelaPrincipal.Hide();
            }
            else
            {
                janelaPrincipal.Show();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            RegistroLog.Registrar("Aplicativo encerrado");
            _mutexPrincipal?.ReleaseMutex();
            _mutexPrincipal?.Dispose();
            base.OnExit(e);
        }
    }
}
