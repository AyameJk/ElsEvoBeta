using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ElsEvo
{
    public static class PromptSimples
    {
        public static string? PedirTexto(Window owner, string titulo, string mensagem, string valorInicial = "")
        {
            bool temaClaro = Properties.Settings.Default.TemaClaro;

            var janela = new Window
            {
                Title = "ElsEvo",
                Width = 360,
                SizeToContent = SizeToContent.Height,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false,
                Background = (Brush)Application.Current.Resources["CorFundoPrincipal"]
            };

            janela.SourceInitialized += (_, _) => BarraTituloNativa.AplicarTema(janela, !temaClaro);

            var raiz = new Grid { Margin = new Thickness(18) };
            raiz.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            raiz.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            raiz.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var linhaTopo = new Grid { Margin = new Thickness(0, 0, 0, 14) };
            linhaTopo.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            linhaTopo.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var iconeFundo = new Border
            {
                Width = 38,
                Height = 38,
                CornerRadius = new CornerRadius(19),
                Background = (Brush)new BrushConverter().ConvertFrom("#0078D4")!,
                Margin = new Thickness(0, 0, 12, 0)
            };
            iconeFundo.Child = new TextBlock
            {
                Text = "\uE70F",
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 16,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(iconeFundo, 0);
            linhaTopo.Children.Add(iconeFundo);

            var painelTextos = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            painelTextos.Children.Add(new TextBlock
            {
                Text = titulo,
                Foreground = (Brush)Application.Current.Resources["CorTextoPrimario"],
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                TextWrapping = TextWrapping.Wrap
            });
            painelTextos.Children.Add(new TextBlock
            {
                Text = mensagem,
                Foreground = (Brush)Application.Current.Resources["CorTextoSecundario"],
                FontSize = 12,
                Margin = new Thickness(0, 4, 0, 0),
                TextWrapping = TextWrapping.Wrap
            });
            Grid.SetColumn(painelTextos, 1);
            linhaTopo.Children.Add(painelTextos);

            Grid.SetRow(linhaTopo, 0);
            raiz.Children.Add(linhaTopo);

            var caixaTexto = new TextBox
            {
                Text = valorInicial,
                Height = 30,
                Padding = new Thickness(8, 0, 0, 0),
                VerticalContentAlignment = VerticalAlignment.Center,
                Background = (Brush)Application.Current.Resources["CorFundoCampo"],
                Foreground = (Brush)Application.Current.Resources["CorTextoPrimario"],
                BorderBrush = (Brush)Application.Current.Resources["CorBorda"],
                BorderThickness = new Thickness(1),
                CaretBrush = (Brush)Application.Current.Resources["CorTextoPrimario"],
                SelectionBrush = (Brush)new BrushConverter().ConvertFrom("#2D5A8C")!
            };
            caixaTexto.SelectAll();
            Grid.SetRow(caixaTexto, 1);
            raiz.Children.Add(caixaTexto);

            string? resultado = null;

            var linhaBotoes = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 16, 0, 0)
            };

            var btnCancelar = CriarBotao("Cancelar", primario: false);
            btnCancelar.Margin = new Thickness(0, 0, 8, 0);
            btnCancelar.Click += (_, _) => { janela.DialogResult = false; janela.Close(); };

            var btnOk = CriarBotao("OK", primario: true);
            btnOk.IsDefault = true;
            btnOk.Click += (_, _) =>
            {
                resultado = caixaTexto.Text;
                janela.DialogResult = true;
                janela.Close();
            };

            linhaBotoes.Children.Add(btnCancelar);
            linhaBotoes.Children.Add(btnOk);

            Grid.SetRow(linhaBotoes, 2);
            raiz.Children.Add(linhaBotoes);

            janela.Content = raiz;
            janela.Loaded += (_, _) => { caixaTexto.Focus(); caixaTexto.SelectAll(); };

            bool? confirmou = janela.ShowDialog();
            return confirmou == true ? resultado : null;
        }

        // Mesmo padrão visual de botão usado na JanelaConfirmacao (arredondado, com hover).
        private static Button CriarBotao(string texto, bool primario)
        {
            var botao = new Button
            {
                Content = texto,
                Width = 90,
                Padding = new Thickness(10, 8, 10, 8),
                Cursor = System.Windows.Input.Cursors.Hand,
                BorderThickness = new Thickness(0)
            };

            if (primario)
            {
                botao.Background = (Brush)new BrushConverter().ConvertFrom("#0078D4")!;
                botao.Foreground = Brushes.White;
                botao.FontWeight = FontWeights.SemiBold;
            }
            else
            {
                botao.Background = (Brush)Application.Current.Resources["CorBotaoFlat"];
                botao.Foreground = (Brush)Application.Current.Resources["CorTextoPrimario"];
            }

            var template = new ControlTemplate(typeof(Button));
            var borda = new FrameworkElementFactory(typeof(Border));
            borda.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
            borda.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            var conteudo = new FrameworkElementFactory(typeof(ContentPresenter));
            conteudo.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            conteudo.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            conteudo.SetBinding(ContentPresenter.MarginProperty, new System.Windows.Data.Binding("Padding") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
            borda.AppendChild(conteudo);
            template.VisualTree = borda;
            botao.Template = template;

            return botao;
        }
    }
}
