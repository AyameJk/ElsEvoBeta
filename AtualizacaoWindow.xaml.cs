using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ElsEvo
{
    public partial class AtualizacaoWindow : Window
    {
        private static readonly Regex RegexElementoEspecial = new(
            @"<img\b[^>]*\bsrc\s*=\s*(?:[""'](?<urlImagem>[^""']+)[""']|(?<urlImagem>[^\s>]+))[^>]*/?>" +
            @"|<blockquote[^>]*>(?<citacaoHtml>[\s\S]*?)</blockquote>" +
            @"|^[ \t]*>[ \t]*(?<citacao>.+)$" +
            @"|^[ \t]*[-*+][ \t]+(?<itemLista>.+)$" +
            @"|<h[1-6][^>]*>(?<tituloHtml>[\s\S]*?)</h[1-6]>" +
            @"|^[ \t]*#{1,6}[ \t]+(?<tituloMd>.+)$",
            RegexOptions.IgnoreCase | RegexOptions.Multiline);

        private static readonly Regex RegexTagsEmbrulho = new(
            @"</?p[^>]*>|</?div[^>]*>|<br\s*/?>",
            RegexOptions.IgnoreCase);

        private static readonly Regex RegexMarkdownInline = new(
            @"\[(?<texto>[^\]]+)\]\((?<url>https?://[^\s)]+)\)|`(?<codigo>[^`]+)`",
            RegexOptions.IgnoreCase);

        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };

        public AtualizacaoWindow(AtualizacaoDisponivel atualizacao)
        {
            InitializeComponent();
            ThemeManager.AplicarTemaSalvo();

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
            string texto = WebUtility.HtmlDecode(notasBrutas ?? string.Empty);

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
                    string citacao = LimparMarkdownSimples(match.Groups["citacao"].Value.Trim().Trim('"', '“', '”'));
                    AdicionarCitacao(citacao);
                    adicionouAlgumElemento = true;
                }
                else if (match.Groups["citacaoHtml"].Success)
                {
                    string citacao = LimparMarkdownSimples(LimparHtmlInterno(match.Groups["citacaoHtml"].Value).Trim('"', '“', '”'));
                    AdicionarCitacao(citacao);
                    adicionouAlgumElemento = true;
                }
                else if (match.Groups["itemLista"].Success)
                {
                    AdicionarItemLista(match.Groups["itemLista"].Value.Trim());
                    adicionouAlgumElemento = true;
                }
                else if (match.Groups["tituloMd"].Success)
                {
                    AdicionarTitulo(match.Groups["tituloMd"].Value.Trim());
                    adicionouAlgumElemento = true;
                }
                else if (match.Groups["tituloHtml"].Success)
                {
                    AdicionarTitulo(LimparHtmlInterno(match.Groups["tituloHtml"].Value));
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

        private static string LimparHtmlInterno(string html)
        {
            string semTags = Regex.Replace(html ?? string.Empty, @"<[^>]+>", " ");
            semTags = WebUtility.HtmlDecode(semTags);
            return Regex.Replace(semTags, @"\s+", " ").Trim();
        }

        private static string LimparMarkdownSimples(string texto)
        {
            return Regex.Replace(texto, @"\*{1,3}|_{1,3}", string.Empty).Trim();
        }

        private bool AdicionarTexto(string trecho)
        {
            string semEmbrulho = RegexTagsEmbrulho.Replace(trecho, string.Empty);
            string limpo = Regex.Replace(semEmbrulho, @"(\r?\n){3,}", "\n\n").Trim();
            if (string.IsNullOrWhiteSpace(limpo))
                return false;

            var texto = new TextBlock
            {
                Foreground = (Brush)FindResource("CorTextoSecundario"),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 17,
                Margin = new Thickness(0, 0, 0, 10)
            };

            AdicionarInlinesFormatados(texto, limpo);
            PainelNotas.Children.Add(texto);
            return true;
        }

        private static void AdicionarInlinesFormatados(TextBlock destino, string texto)
        {
            int posicaoAtual = 0;
            foreach (Match match in RegexMarkdownInline.Matches(texto))
            {
                if (match.Index > posicaoAtual)
                    destino.Inlines.Add(new Run(WebUtility.HtmlDecode(texto.Substring(posicaoAtual, match.Index - posicaoAtual))));

                if (match.Groups["url"].Success)
                {
                    var link = new Hyperlink(new Run(WebUtility.HtmlDecode(match.Groups["texto"].Value)))
                    {
                        NavigateUri = new Uri(match.Groups["url"].Value),
                        Foreground = new SolidColorBrush(Color.FromRgb(99, 179, 237))
                    };
                    link.RequestNavigate += AbrirLink;
                    destino.Inlines.Add(link);
                }
                else
                {
                    destino.Inlines.Add(new InlineUIContainer
                    {
                        BaselineAlignment = BaselineAlignment.Center,
                        Child = new Border
                        {
                            Background = new SolidColorBrush(Color.FromRgb(37, 42, 51)),
                            CornerRadius = new CornerRadius(3),
                            Padding = new Thickness(4, 1, 4, 1),
                            Child = new TextBlock
                            {
                                Text = WebUtility.HtmlDecode(match.Groups["codigo"].Value),
                                Foreground = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                                FontSize = 11
                            }
                        }
                    });
                }
                posicaoAtual = match.Index + match.Length;
            }

            if (posicaoAtual < texto.Length)
                destino.Inlines.Add(new Run(WebUtility.HtmlDecode(texto.Substring(posicaoAtual))));
        }

        private static void AbrirLink(object sender, RequestNavigateEventArgs evento)
        {
            if (evento.Uri.Scheme is "http" or "https")
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(evento.Uri.AbsoluteUri) { UseShellExecute = true });
                evento.Handled = true;
            }
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
                Text = WebUtility.HtmlDecode(titulo),
                Foreground = (Brush)FindResource("CorTextoPrimario"),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8)
            });
        }

        private void AdicionarItemLista(string item)
        {
            if (string.IsNullOrWhiteSpace(item))
                return;

            var texto = new TextBlock
            {
                Foreground = (Brush)FindResource("CorTextoSecundario"),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 17,
                Margin = new Thickness(0, 0, 0, 4)
            };
            texto.Inlines.Add(new Run("• "));
            AdicionarInlinesFormatados(texto, LimparMarkdownSimples(item));
            PainelNotas.Children.Add(texto);
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
