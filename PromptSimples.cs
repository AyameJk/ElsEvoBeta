using System.Windows;
using System.Windows.Controls;

namespace ElsEvo
{
    public static class PromptSimples
    {
        public static string? PedirTexto(Window owner, string titulo, string mensagem, string valorInicial = "")
        {
            var janela = new Window
            {
                Title = titulo,
                Width = 320,
                Height = 160,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                Background = System.Windows.Media.Brushes.Black
            };

            var painel = new StackPanel { Margin = new Thickness(16) };

            var textoMensagem = new TextBlock
            {
                Text = mensagem,
                Foreground = System.Windows.Media.Brushes.White,
                Margin = new Thickness(0, 0, 0, 8),
                TextWrapping = TextWrapping.Wrap
            };

            var caixaTexto = new TextBox
            {
                Text = valorInicial,
                Height = 28,
                Padding = new Thickness(6, 0, 0, 0),
                VerticalContentAlignment = VerticalAlignment.Center
            };

            string? resultado = null;

            var linhaBotoes = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 16, 0, 0)
            };

            var btnCancelar = new Button { Content = "Cancelar", Width = 80, Margin = new Thickness(0, 0, 8, 0) };
            var btnOk = new Button { Content = "OK", Width = 80, IsDefault = true };

            btnCancelar.Click += (_, _) => { janela.DialogResult = false; janela.Close(); };
            btnOk.Click += (_, _) =>
            {
                resultado = caixaTexto.Text;
                janela.DialogResult = true;
                janela.Close();
            };

            linhaBotoes.Children.Add(btnCancelar);
            linhaBotoes.Children.Add(btnOk);

            painel.Children.Add(textoMensagem);
            painel.Children.Add(caixaTexto);
            painel.Children.Add(linhaBotoes);

            janela.Content = painel;

            bool? confirmou = janela.ShowDialog();
            return confirmou == true ? resultado : null;
        }
    }
}
