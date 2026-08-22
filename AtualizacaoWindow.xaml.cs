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
    /// <summary>
    /// Substitui o MessageBox nativo do fluxo "uma atualização está disponível" por uma
    /// janela que segue o tema Claro/Escuro do app (mesmos DynamicResource usados no
    /// resto da interface — CorFundoPrincipal, CorBotaoFlat, etc., ver ThemeManager.cs).
    /// DialogResult == true significa "o usuário quer atualizar agora".
    ///
    /// As notas de lançamento vêm em Markdown/HTML puro do GitHub Releases (podem ter uma
    /// tag &lt;img&gt; de capa, uma linha de citação em "&gt; texto", e títulos "#"/"##"/"###"),
    /// em QUALQUER ordem dentro do texto. Como o TextBlock não renderiza Markdown/HTML, essa
    /// janela monta o conteúdo dinamicamente em PainelNotas, elemento por elemento,
    /// respeitando a MESMA ORDEM em que cada trecho aparece no texto original.
    /// </summary>
    public partial class AtualizacaoWindow : Window
    {
        // Casa <img ... src="URL" ...>, uma linha "> texto" (citação), OU uma linha
        // "#"/"##"/"###" de título Markdown — o que vier primeiro no texto processa
        // primeiro, preservando a ordem original.
        private static readonly Regex RegexElementoEspecial = new(
            @"<img[^>]*\ssrc=[""'](?<urlImagem>[^""']+)[""'][^>]*/?>" +
            @"|^\s*>\s*(?<citacao>.+)$" +
            @"|^\s*#{1,6}\s*(?<titulo>.+?)\s*#*$",
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

        /// <summary>Aplica as strings traduzidas nos elementos fixos da janela (não nas notas
        /// da release, que vêm do GitHub no idioma que foi escrito e não são traduzidas).</summary>
        private void AplicarIdioma()
        {
            Title = Idiomas.T("AtualizacaoTitulo");
            TxtTitulo.Text = Idiomas.T("AtualizacaoTitulo");
            TxtAvisoBeta.Text = Idiomas.T("AtualizacaoAvisoBeta");
            TxtAvisoFechamento.Text = Idiomas.T("AtualizacaoAvisoFechamento");
            BtnAgoraNao.Content = Idiomas.T("AtualizacaoBtnAgoraNao");
            BtnAtualizar.Content = Idiomas.T("AtualizacaoBtnAtualizar");
        }

        /// <summary>
        /// Ajusta as cores do aviso de canal beta conforme o tema atual. No tema Escuro
        /// mantém o aviso âmbar/amarelado de sempre (mistura bem com o fundo escuro). No
        /// tema Claro, muda pra vermelho — mesma linguagem visual do badge "DESLIGADO" —
        /// porque o âmbar clarinho ficava com contraste ruim e sem aparência de "aviso"
        /// de verdade em cima de um fundo claro.
        /// </summary>
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

        /// <summary>
        /// Percorre as notas em ordem, transformando cada trecho de texto normal, cada
        /// &lt;img&gt;, cada linha de citação e cada título Markdown num elemento visual,
        /// na mesma sequência em que aparecem no Markdown original — sem reordenar nada.
        /// </summary>
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
                // Texto normal ANTES desse elemento especial, na ordem em que aparece.
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

            // Texto normal depois do último elemento especial encontrado.
            if (posicaoAtual < texto.Length)
            {
                string trechoFinal = texto.Substring(posicaoAtual);
                if (AdicionarTexto(trechoFinal))
                    adicionouAlgumElemento = true;
            }

            if (!adicionouAlgumElemento)
                AdicionarTexto(Idiomas.T("AtualizacaoSemNotas"));
        }

        /// <summary>Adiciona um trecho de texto normal (se não for só espaço em branco).</summary>
        private bool AdicionarTexto(string trecho)
        {
            string limpo = Regex.Replace(trecho, @"(\r?\n){3,}", "\n\n").Trim();
            if (string.IsNullOrWhiteSpace(limpo))
                return false;

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

        /// <summary>Adiciona o espaço reservado da imagem e dispara o download em segundo plano.</summary>
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

        /// <summary>Adiciona um título Markdown (#, ##, ### — todos tratados igual, sem
        /// distinguir hierarquia) como texto em destaque, negrito e maior que o corpo.</summary>
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

        /// <summary>Adiciona a citação já destacada (barrinha lateral + itálico), sem o "&gt;" cru.</summary>
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

        /// <summary>
        /// Baixa a imagem em segundo plano e mostra ela de verdade no lugar reservado.
        /// Se falhar por qualquer motivo, o espaço reservado simplesmente continua
        /// invisível — nunca trava nem quebra a janela.
        /// </summary>
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
                // Sem imagem — a janela continua funcionando normalmente sem ela.
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
