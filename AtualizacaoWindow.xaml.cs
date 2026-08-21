using System;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
    /// tag &lt;img&gt; de capa e uma linha de citação em "&gt; texto"), em QUALQUER ordem
    /// dentro do texto. Como o TextBlock não renderiza Markdown/HTML, essa janela monta o
    /// conteúdo dinamicamente em PainelNotas, elemento por elemento, respeitando a MESMA
    /// ORDEM em que cada trecho aparece no texto original — texto vira TextBlock normal,
    /// &lt;img&gt; vira uma imagem de verdade baixada da URL, e "&gt; texto" vira um bloco
    /// de citação destacado. Qualquer coisa que não seja reconhecida simplesmente não
    /// aparece destacada — nunca quebra a janela.
    /// </summary>
    public partial class AtualizacaoWindow : Window
    {
        // Casa <img ... src="URL" ...> OU uma linha "> texto" (citação) — o que vier
        // primeiro no texto processa primeiro, preservando a ordem original.
        private static readonly Regex RegexElementoEspecial = new(
            @"<img[^>]*\ssrc=[""'](?<urlImagem>[^""']+)[""'][^>]*/?>" +
            @"|^\s*>\s*(?<citacao>.+)$",
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
        /// Percorre as notas em ordem, transformando cada trecho de texto normal, cada
        /// &lt;img&gt; e cada linha de citação num elemento visual, na mesma sequência em
        /// que aparecem no Markdown original — sem reordenar nada.
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
                    string citacao = match.Groups["citacao"].Value.Trim().Trim('"', '“', '”');
                    AdicionarCitacao(citacao);
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
                Foreground = (System.Windows.Media.Brush)FindResource("CorTextoSecundario"),
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
                Stretch = System.Windows.Media.Stretch.Uniform,
                MaxHeight = 220
            };
            System.Windows.Media.RenderOptions.SetBitmapScalingMode(imagem, System.Windows.Media.BitmapScalingMode.HighQuality);

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

        /// <summary>Adiciona a citação já destacada (barrinha lateral + itálico), sem o "&gt;" cru.</summary>
        private void AdicionarCitacao(string citacao)
        {
            if (string.IsNullOrWhiteSpace(citacao))
                return;

            var container = new Border
            {
                BorderBrush = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#0078D4")!,
                BorderThickness = new Thickness(3, 0, 0, 0),
                Padding = new Thickness(10, 4, 10, 4),
                Margin = new Thickness(0, 0, 0, 10),
                Child = new TextBlock
                {
                    Text = $"“{citacao}”",
                    Foreground = (System.Windows.Media.Brush)FindResource("CorTextoPrimario"),
                    FontSize = 12,
                    FontStyle = System.Windows.FontStyles.Italic,
                    TextWrapping = TextWrapping.Wrap
                }
            };

            PainelNotas.Children.Add(container);
        }

        /// <summary>
        /// Baixa a imagem em segundo plano e mostra ela de verdade no lugar reservado.
        /// Se falhar por qualquer motivo (sem internet, URL inválida, etc.), o espaço
        /// reservado simplesmente continua invisível — nunca trava nem quebra a janela.
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
