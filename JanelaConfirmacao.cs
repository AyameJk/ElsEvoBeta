using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ElsEvo
{
    public enum TipoMensagem
    {
        Informacao,
        Sucesso,
        Aviso,
        Erro
    }

    public static class JanelaConfirmacao
    {
        public static void Mostrar(Window owner, string titulo, string mensagem, TipoMensagem tipo = TipoMensagem.Informacao)
        {
            Construir(owner, titulo, mensagem, tipo, comBotaoCancelar: false).ShowDialog();
        }

        public static bool Confirmar(Window owner, string titulo, string mensagem, TipoMensagem tipo = TipoMensagem.Aviso)
        {
            var janela = Construir(owner, titulo, mensagem, tipo, comBotaoCancelar: true);
            return janela.ShowDialog() == true;
        }

        private static (string Glifo, string CorFundo) DadosPorTipo(TipoMensagem tipo) => tipo switch
        {
            TipoMensagem.Sucesso => ("\uE73E", "#2E7D32"),
            TipoMensagem.Aviso => ("\uE7BA", "#B8860B"),
            TipoMensagem.Erro => ("\uE783", "#C62828"),
            _ => ("\uE946", "#0078D4")
        };

        private static Window Construir(Window owner, string titulo, string mensagem, TipoMensagem tipo, bool comBotaoCancelar)
        {
            var (glifo, corIcone) = DadosPorTipo(tipo);
            bool temaClaro = Properties.Settings.Default.TemaClaro;

            var janela = new Window
            {
                Title = "ElsEvo",
                Width = 380,
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

            var linhaTopo = new Grid { Margin = new Thickness(0, 0, 0, 16) };
            linhaTopo.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            linhaTopo.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var iconeFundo = new Border
            {
                Width = 38,
                Height = 38,
                CornerRadius = new CornerRadius(19),
                Background = (Brush)new BrushConverter().ConvertFrom(corIcone)!,
                Margin = new Thickness(0, 0, 12, 0)
            };
            iconeFundo.Child = new TextBlock
            {
                Text = glifo,
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 17,
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

            var linhaBotoes = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            if (comBotaoCancelar)
            {
                var btnNao = CriarBotao("Não", primario: false);
                btnNao.Margin = new Thickness(0, 0, 8, 0);
                btnNao.Click += (_, _) => { janela.DialogResult = false; janela.Close(); };
                linhaBotoes.Children.Add(btnNao);

                var btnSim = CriarBotao("Sim", primario: true);
                btnSim.Click += (_, _) => { janela.DialogResult = true; janela.Close(); };
                linhaBotoes.Children.Add(btnSim);
            }
            else
            {
                var btnOk = CriarBotao("OK", primario: true);
                btnOk.Click += (_, _) => { janela.DialogResult = true; janela.Close(); };
                linhaBotoes.Children.Add(btnOk);
            }

            Grid.SetRow(linhaBotoes, 1);
            raiz.Children.Add(linhaBotoes);

            janela.Content = raiz;
            return janela;
        }

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
