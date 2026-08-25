using System;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ElsEvo
{
    public partial class AtualizacaoWindow : Window
    {
        private static readonly Regex RegexElementoEspecial = new(
            @"<img[^>]*\ssrc=[""'](?<urlImagem>[^""']+)[""'][^>]*/?>" +
            @"|^\s*>\s*(?<citacao>.+)$" +
            @"|^\s*#{1,6}\s*(?<titulo>.+?)\s*#*$" +
            @"|(?<tagIgnorada></?(?:p|div|span|br|hr|center)(?:\s[^>]*)?/?>)",
            RegexOptions.IgnoreCase | RegexOptions.Multiline);

        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };

        public AtualizacaoWindow(AtualizacaoDisponivel atualizacao)
        {
            InitializeComponent();
            ThemeManager.AplicarTemaSalvo(); // reforço de segurança, igual as outras janelas

            SourceInitialized += (_, _) =>
                BarraTituloNativa.AplicarTema(this, !Properties.Settings.Default.TemaClaro);

            TxtVersaoNova.Text = string.Format(Idiomas.T("AtualizacaoVersaoDisponivel"), atualizacao.VersaoNova);

            ContainerAvisoBeta.Visibility = atualizacao.EhCanalBeta ? Visibility.Visible : Visibility.Collapsed;
            AtualizarCorAvisoBeta();

            AplicarIdioma();
            PrepararNotas(atualizacao.Notas);
        }

        private void AplicarIdioma()
        {
            Title = Idiomas.T("AtualizacaoTitulo");
            TxtTitulo.Text = Idiomas.T("AtualizacaoTitulo");
            TxtAvisoBeta.Text = Idiomas.T("AtualizacaoAvisoBeta");
            TxtAvisoFechamento.Text = Idiomas.T("AtualizacaoAvisoFechamento");
            BtnAgoraNao.Content = Idiomas.T("AtualizacaoBtnAgoraNao");
            BtnAtualizar.Content = Idiomas.T("AtualizacaoBtnAtualizar");
        }

        private void AtualizarCorAvisoBeta()
        {
            var bc = new BrushConverter();
            bool temaClaro = Properties.Settings.Default.TemaClaro;

            if (temaClaro)
            {
                ContainerAvisoBeta.Background = (Brush)bc.ConvertFrom("#FDEAEA")!;
                ContainerAvisoBeta.BorderBrush = (Brush)bc.ConvertFrom("#E36464")!;
                IconeAvisoBeta.Foreground = (Brush)bc.ConvertFrom("#C62828")!;
                TxtAvisoBeta.Foreground = (Brush)bc.ConvertFrom("#B71C1C")!;
            }
            else
            {
                ContainerAvisoBeta.Background = (Brush)bc.ConvertFrom("#3D2E1A")!;
                ContainerAvisoBeta.BorderBrush = (Brush)bc.ConvertFrom("#8A6D3B")!;
                IconeAvisoBeta.Foreground = (Brush)bc.ConvertFrom("#F5C542")!;
                TxtAvisoBeta.Foreground = (Brush)bc.ConvertFrom("#E3D2A8")!;
            }
        }

        private void PrepararNotas(string notasBrutas)
        {
            string texto = notasBrutas ?? string.Empty;

            var matches = RegexElementoEspecial.Matches(texto);

            if (matches.Count == 0)
            {
                AdicionarTexto(texto);
                return;
            }

            int posicaoAtual = 0;
            bool adicionouAlgumElemento = false;

            foreach (Match match in matches)
            {
                if (match.Index > posicaoAtual)
                {
                    string trecho = texto.Substring(posicaoAtual, match.Index - posicaoAtual);
                    if (AdicionarTexto(trecho))
                        adicionouAlgumElemento = true;
                }

                if (match.Groups["urlImagem"].Success)
                {
                    AdicionarImagem(match.Groups["urlImagem"].Value);
                    adicionouAlgumElemento = true;
                }
                else if (match.Groups["citacao"].Success)
                {
                    string citacao = match.Groups["citacao"].Value.Trim().Trim('"', '\u201c', '\u201d');
                    AdicionarCitacao(citacao);
                    adicionouAlgumElemento = true;
                }
                else if (match.Groups["titulo"].Success)
                {
                    AdicionarTitulo(match.Groups["titulo"].Value.Trim());
                    adicionouAlgumElemento = true;
                }

                posicaoAtual = match.Index + match.Length;
            }

            if (posicaoAtual < texto.Length)
            {
                string trechoFinal = texto.Substring(posicaoAtual);
                if (AdicionarTexto(trechoFinal))
                    adicionouAlgumElemento = true;
            }

            if (!adicionouAlgumElemento)
                AdicionarTexto(Idiomas.T("AtualizacaoSemNotas"));
        }

        private static readonly Regex RegexItemDeLista = new(@"^[ \t]*[-*][ \t]+", RegexOptions.Multiline);

        private bool AdicionarTexto(string trecho)
        {
            string limpo = Regex.Replace(trecho, @"(\r?\n){3,}", "\n\n").Trim();
            if (string.IsNullOrWhiteSpace(limpo))
                return false;

            limpo = RegexItemDeLista.Replace(limpo, "• ");

            PainelNotas.Children.Add(new TextBlock
            {
                Text = limpo,
                Foreground = (Brush)FindResource("CorTextoSecundario"),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 17,
                Margin = new Thickness(0, 0, 0, 10)
            });
            return true;
        }

        private void AdicionarImagem(string url)
        {
            var imagem = new Image
            {
                Stretch = Stretch.Uniform,
                MaxHeight = 220
            };
            RenderOptions.SetBitmapScalingMode(imagem, BitmapScalingMode.HighQuality);

            var container = new Border
            {
                CornerRadius = new CornerRadius(6),
                ClipToBounds = true,
                Margin = new Thickness(0, 0, 0, 10),
                Visibility = Visibility.Collapsed,
                Child = imagem
            };

            PainelNotas.Children.Add(container);

            if (!string.IsNullOrWhiteSpace(url))
                _ = CarregarImagemAsync(url, imagem, container);
        }

        private void AdicionarTitulo(string titulo)
        {
            if (string.IsNullOrWhiteSpace(titulo))
                return;

            PainelNotas.Children.Add(new TextBlock
            {
                Text = titulo,
                Foreground = (Brush)FindResource("CorTextoPrimario"),
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, 0, 6)
            });
        }

        private void AdicionarCitacao(string citacao)
        {
            if (string.IsNullOrWhiteSpace(citacao))
                return;

            var container = new Border
            {
                BorderBrush = (Brush)new BrushConverter().ConvertFrom("#0078D4")!,
                BorderThickness = new Thickness(3, 0, 0, 0),
                Padding = new Thickness(10, 4, 10, 4),
                Margin = new Thickness(0, 0, 0, 10),
                Child = new TextBlock
                {
                    Text = $"\u201c{citacao}\u201d",
                    Foreground = (Brush)FindResource("CorTextoPrimario"),
                    FontSize = 12,
                    FontStyle = FontStyles.Italic,
                    TextWrapping = TextWrapping.Wrap
                }
            };

            PainelNotas.Children.Add(container);
        }

        private async Task CarregarImagemAsync(string url, Image imagem, Border container)
        {
            try
            {
                byte[] dados = await _http.GetByteArrayAsync(url);

                var bitmap = new BitmapImage();
                using (var stream = new MemoryStream(dados))
                {
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                }
                bitmap.Freeze();

                imagem.Source = bitmap;
                container.Visibility = Visibility.Visible;
            }
            catch
            {
            }
        }

        private void BtnAtualizar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void BtnAgoraNao_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
