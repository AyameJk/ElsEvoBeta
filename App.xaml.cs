using System;
using System.Threading;
using System.Windows;
using ElsEvo.Properties;

namespace ElsEvo
{
    public partial class App : Application
    {
        private static Mutex? _mutexPrincipal;

        private static bool _mensagemDeErroJaExibida;

        private static string DetalharExcecao(Exception ex)
        {
            string texto = $"{ex.GetType().Name}: {ex.Message}";
            var interna = ex.InnerException;
            while (interna != null)
            {
                texto += $"\n  → Causa: {interna.GetType().Name}: {interna.Message}";
                interna = interna.InnerException;
            }
            return texto;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            RegistroLog.Registrar("Aplicativo iniciado");

            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                var ex = args.ExceptionObject as Exception;
                string detalhes = ex != null ? DetalharExcecao(ex) : "Objeto de exceção desconhecido";
                RegistroLog.Registrar("Exceção não tratada (AppDomain)", detalhes);

                if (!_mensagemDeErroJaExibida)
                {
                    _mensagemDeErroJaExibida = true;
                    MessageBox.Show(
                        $"Ocorreu um erro inesperado e o ElsEvo vai fechar:\n\n{detalhes}\n\nDetalhes em app-log.txt.",
                        "ElsEvo — erro fatal", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            DispatcherUnhandledException += (_, args) =>
            {
                string detalhes = DetalharExcecao(args.Exception);
                RegistroLog.Registrar("Exceção não tratada (Dispatcher)", detalhes);

                if (!_mensagemDeErroJaExibida)
                {
                    _mensagemDeErroJaExibida = true;
                    MessageBox.Show(
                        $"Ocorreu um erro inesperado:\n\n{detalhes}\n\nDetalhes em app-log.txt.",
                        "ElsEvo — erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                args.Handled = true;
            };

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
