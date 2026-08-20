using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace ElsEVO
{
    public partial class GerenciarModsWindow : Window
    {
        private readonly List<ModItem> _todosOsMods = new();

        private const string OpcaoDesabilitarTodos = "Desabilitar todos";

        public GerenciarModsWindow()
        {
            InitializeComponent();
            ThemeManager.AplicarTemaSalvo();
            CarregarModsJaImportados();
            AtualizarComboGlobal();
            AplicarFiltro();
            AplicarIdioma();
            InscreverAlteracoes(_todosOsMods);

            SourceInitialized += (_, _) =>
                BarraTituloNativa.AplicarTema(this, !Properties.Settings.Default.TemaClaro);

            BtnAplicar.IsEnabled = false;
        }

        private void MarcarAlteracaoPendente() => BtnAplicar.IsEnabled = true;

        private void InscreverAlteracoes(IEnumerable<ModItem> itens)
        {
            foreach (var item in itens)
                item.PropertyChanged += (_, _) => MarcarAlteracaoPendente();
        }

        private void AplicarIdioma()
        {
            Title = Idiomas.T("TituloGerenciarMods");
            BtnOk.Content = Idiomas.T("BotaoOk");
            BtnCancelar.Content = Idiomas.T("BotaoCancelar");
            BtnAplicar.Content = Idiomas.T("BotaoAplicar");
        }

        private void CarregarModsJaImportados()
        {
            var ativos = GerenciadorDeMods.Carregar()
                .ToDictionary(a => a.Arquivo, a => a.NomeDoPack, StringComparer.OrdinalIgnoreCase);

            var porArquivo = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            if (Directory.Exists(Paths.Main.Packs))
            {
                foreach (var pastaPack in Directory.GetDirectories(Paths.Main.Packs))
                {
                    string nomePack = Path.GetFileName(pastaPack);

                    foreach (var caminhoArquivo in Directory.GetFiles(pastaPack))
                    {
                        string nomeArquivo = Path.GetFileName(caminhoArquivo);

                        if (!porArquivo.TryGetValue(nomeArquivo, out var lista))
                        {
                            lista = new List<string>();
                            porArquivo[nomeArquivo] = lista;
                        }

                        lista.Add(nomePack);
                    }
                }
            }

            foreach (var (nomeArquivo, packsComEsseArquivo) in porArquivo)
            {
                string modSelecionado = ativos.TryGetValue(nomeArquivo, out var packAtivo)
                                         && packsComEsseArquivo.Contains(packAtivo, StringComparer.OrdinalIgnoreCase)
                    ? packAtivo
                    : "Nenhum";

                var conhecido = BancoDeArquivos.BuscarPorNome(nomeArquivo);
                string nomePackParaDescricao = modSelecionado != "Nenhum" ? modSelecionado : packsComEsseArquivo.First();
                string descricao = conhecido != null
                    ? conhecido.Description
                    : $"[{nomePackParaDescricao}] {Path.GetFileNameWithoutExtension(nomeArquivo)}";

                var opcoes = new List<string> { "Nenhum" };
                opcoes.AddRange(packsComEsseArquivo.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(p => p));

                _todosOsMods.Add(new ModItem
                {
                    Arquivo = nomeArquivo,
                    Descricao = descricao,
                    Categoria = CategoriaPorExtensao(nomeArquivo),
                    CaminhoCompleto = Path.Combine(Paths.Main.Packs, nomePackParaDescricao, nomeArquivo),
                    OpcoesDisponiveis = opcoes,
                    ModSelecionado = modSelecionado
                });
            }
        }

        private List<string> ObterPacksParaArquivo(string nomeArquivo, string? garantirIncluido = null)
        {
            var opcoes = new List<string> { "Nenhum" };

            if (Directory.Exists(Paths.Main.Packs))
            {
                foreach (var pastaPack in Directory.GetDirectories(Paths.Main.Packs))
                {
                    string nomePack = Path.GetFileName(pastaPack);
                    string possivelArquivo = Path.Combine(pastaPack, nomeArquivo);
                    if (File.Exists(possivelArquivo))
                        opcoes.Add(nomePack);
                }
            }

            if (garantirIncluido != null && !opcoes.Contains(garantirIncluido))
                opcoes.Add(garantirIncluido);

            return opcoes;
        }

        private void AtualizarOpcoesDeTodasAsLinhas()
        {
            foreach (var mod in _todosOsMods)
                mod.OpcoesDisponiveis = ObterPacksParaArquivo(mod.Arquivo, mod.ModSelecionado);
        }

        private void AtualizarComboGlobal()
        {
            CmbAcaoGlobal.Items.Clear();
            CmbAcaoGlobal.Items.Add(OpcaoDesabilitarTodos);

            var packs = _todosOsMods
                .SelectMany(m => m.OpcoesDisponiveis)
                .Where(p => p != "Nenhum")
                .Distinct()
                .OrderBy(p => p);

            foreach (var pack in packs)
                CmbAcaoGlobal.Items.Add(pack);

            CmbAcaoGlobal.SelectedIndex = 0;
        }

        private static string CategoriaPorExtensao(string caminhoArquivo)
        {
            string ext = Path.GetExtension(caminhoArquivo).ToLowerInvariant();
            if (ext == ".ogg") return "BGM";
            if (ext == ".avi") return "Video";
            return "Geral";
        }

        private void AplicarFiltro()
        {
            string categoria = TabCategorias.SelectedIndex switch
            {
                1 => "BGM",
                2 => "Video",
                _ => "Geral"
            };

            string termoBusca = TxtPesquisa.Text?.Trim() ?? string.Empty;

            var filtrados = _todosOsMods
                .Where(m => m.Categoria == categoria)
                .Where(m => string.IsNullOrEmpty(termoBusca)
                            || m.Arquivo.Contains(termoBusca, StringComparison.OrdinalIgnoreCase)
                            || m.Descricao.Contains(termoBusca, StringComparison.OrdinalIgnoreCase))
                .ToList();

            GridMods.ItemsSource = new ObservableCollection<ModItem>(filtrados);
        }

        private void TabCategorias_SelectionChanged(object sender, SelectionChangedEventArgs e) => AplicarFiltro();

        private void TxtPesquisa_TextChanged(object sender, TextChangedEventArgs e) => AplicarFiltro();

        private void BtnMenu_Click(object sender, RoutedEventArgs e) => PopupMenu.IsOpen = !PopupMenu.IsOpen;

        private void MenuNovo_Click(object sender, RoutedEventArgs e)
        {
            PopupMenu.IsOpen = false;

            string? nomeEscolhido = PromptSimples.PedirTexto(this, "Novo pack", "Nome do novo pack de mod:", "MeuMod");
            if (string.IsNullOrWhiteSpace(nomeEscolhido))
                return;

            string pastaPack = Path.Combine(Paths.Main.Packs, nomeEscolhido);
            Directory.CreateDirectory(pastaPack);

            MessageBox.Show(
                $"Pack \"{nomeEscolhido}\" criado em:\n{pastaPack}\n\nAdicione arquivos .kom/.ogg/.avi dentro dessa pasta e importe-a com \"Importar pasta\".",
                "Novo pack", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MenuImportarPasta_Click(object sender, RoutedEventArgs e)
        {
            PopupMenu.IsOpen = false;

            var dialogo = new OpenFolderDialog { Title = "Selecione a pasta do mod para importar" };
            if (dialogo.ShowDialog() != true)
                return;

            _ = ImportarPastaComoPackAsync(dialogo.FolderName);
        }

        private async Task ImportarPastaComoPackAsync(string pastaOrigem)
        {
            if (!Directory.Exists(pastaOrigem))
                return;

            string nomePack = Path.GetFileName(pastaOrigem.TrimEnd(Path.DirectorySeparatorChar));
            string pastaDestino = Path.Combine(Paths.Main.Packs, nomePack);
            Directory.CreateDirectory(pastaDestino);

            var arquivos = Directory.GetFiles(pastaOrigem, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".kom") || f.EndsWith(".ogg") || f.EndsWith(".avi")
                            || Path.GetFileName(f).Equals("general.ess", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (arquivos.Length == 0)
            {
                MessageBox.Show("Nenhum arquivo .kom, .ogg, .avi ou general.ess encontrado nessa pasta.",
                    "Importar pasta", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ProgressoImportacaoContainer.Visibility = Visibility.Visible;
            var novosItens = new List<ModItem>();

            for (int i = 0; i < arquivos.Length; i++)
            {
                string caminhoOrigem = arquivos[i];
                string nomeArquivo = Path.GetFileName(caminhoOrigem);
                string caminhoNoPack = Path.Combine(pastaDestino, nomeArquivo);

                await Task.Run(() => File.Copy(caminhoOrigem, caminhoNoPack, overwrite: true));

                var conhecido = BancoDeArquivos.BuscarPorNome(nomeArquivo);
                string descricao = conhecido != null
                    ? conhecido.Description
                    : $"[{nomePack}] {Path.GetFileNameWithoutExtension(nomeArquivo)}";

                novosItens.Add(new ModItem
                {
                    Arquivo = nomeArquivo,
                    Descricao = descricao,
                    Categoria = CategoriaPorExtensao(nomeArquivo),
                    CaminhoCompleto = caminhoNoPack,
                    ModSelecionado = nomePack
                });

                int percentual = (i + 1) * 100 / arquivos.Length;
                BarraProgressoImportacao.Value = percentual;
                TxtProgressoImportacao.Text = $"Importando arquivos... {percentual}% ({i + 1}/{arquivos.Length})";
            }

            _todosOsMods.AddRange(novosItens);
            InscreverAlteracoes(novosItens);
            AtualizarOpcoesDeTodasAsLinhas();
            AtualizarComboGlobal();
            AplicarFiltro();
            MarcarAlteracaoPendente();

            ProgressoImportacaoContainer.Visibility = Visibility.Collapsed;
        }

        private void MenuImportarZip_Click(object sender, RoutedEventArgs e)
        {
            PopupMenu.IsOpen = false;

            var dialogo = new OpenFileDialog
            {
                Title = "Selecione o arquivo .zip do mod",
                Filter = "Arquivos ZIP (*.zip)|*.zip"
            };

            if (dialogo.ShowDialog() != true)
                return;

            _ = ImportarZipComoPackAsync(dialogo.FileName);
        }

        private async Task ImportarZipComoPackAsync(string caminhoZip)
        {
            string nomePack = Path.GetFileNameWithoutExtension(caminhoZip);
            string pastaDestino = Path.Combine(Paths.Main.Packs, nomePack);

            ProgressoImportacaoContainer.Visibility = Visibility.Visible;
            TxtProgressoImportacao.Text = "Extraindo .zip...";
            BarraProgressoImportacao.Value = 0;

            try
            {
                Directory.CreateDirectory(pastaDestino);
                await Task.Run(() =>
                    System.IO.Compression.ZipFile.ExtractToDirectory(caminhoZip, pastaDestino, overwriteFiles: true));
            }
            catch (Exception ex)
            {
                ProgressoImportacaoContainer.Visibility = Visibility.Collapsed;
                MessageBox.Show($"Não foi possível extrair o zip:\n{ex.Message}\n\n" +
                    "Se o zip tiver senha, ainda não é suportado.",
                    "Importar .zip", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var arquivos = Directory.GetFiles(pastaDestino, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".kom") || f.EndsWith(".ogg") || f.EndsWith(".avi")
                            || Path.GetFileName(f).Equals("general.ess", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            for (int i = 0; i < arquivos.Length; i++)
            {
                string nomeArquivo = Path.GetFileName(arquivos[i]);
                var conhecido = BancoDeArquivos.BuscarPorNome(nomeArquivo);
                string descricao = conhecido != null
                    ? conhecido.Description
                    : $"[{nomePack}] {Path.GetFileNameWithoutExtension(nomeArquivo)}";

                var novoItem = new ModItem
                {
                    Arquivo = nomeArquivo,
                    Descricao = descricao,
                    Categoria = CategoriaPorExtensao(nomeArquivo),
                    CaminhoCompleto = arquivos[i],
                    ModSelecionado = nomePack
                };
                _todosOsMods.Add(novoItem);
                InscreverAlteracoes(new[] { novoItem });

                int percentual = (i + 1) * 100 / Math.Max(arquivos.Length, 1);
                BarraProgressoImportacao.Value = percentual;
                TxtProgressoImportacao.Text = $"Registrando arquivos... {percentual}%";
            }

            AtualizarOpcoesDeTodasAsLinhas();
            AtualizarComboGlobal();
            AplicarFiltro();
            MarcarAlteracaoPendente();

            ProgressoImportacaoContainer.Visibility = Visibility.Collapsed;
        }

        private void MenuExcluirPackSelecionado_Click(object sender, RoutedEventArgs e)
        {
            PopupMenu.IsOpen = false;

            string? nomePack = null;

            if (CmbAcaoGlobal.SelectedItem is string selecionadoGlobal && selecionadoGlobal != OpcaoDesabilitarTodos)
            {
                nomePack = selecionadoGlobal;
            }
            else if (GridMods.SelectedItem is ModItem itemSelecionado && itemSelecionado.ModSelecionado != "Nenhum")
            {
                nomePack = itemSelecionado.ModSelecionado;
            }

            if (nomePack == null)
            {
                MessageBox.Show(
                    "Selecione um pack específico no combo do topo, ou uma linha com um pack definido, antes de excluir.",
                    "Excluir pack", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var resposta = MessageBox.Show(
                $"Isso vai excluir o pack \"{nomePack}\" (e todos os arquivos dele) permanentemente. Continuar?",
                "Excluir pack", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (resposta != MessageBoxResult.Yes)
                return;

            try
            {
                string pastaPack = Path.Combine(Paths.Main.Packs, nomePack);
                if (Directory.Exists(pastaPack))
                    Directory.Delete(pastaPack, recursive: true);

                _todosOsMods.RemoveAll(m => m.ModSelecionado == nomePack || m.CaminhoCompleto.StartsWith(pastaPack));

                AtualizarOpcoesDeTodasAsLinhas();
                AtualizarComboGlobal();
                AplicarFiltro();
                MarcarAlteracaoPendente();

                MessageBox.Show($"Pack \"{nomePack}\" excluído.", "Excluir pack",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Não foi possível excluir o pack:\n{ex.Message}",
                    "Excluir pack", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAplicarAcaoGlobal_Click(object sender, RoutedEventArgs e)
        {
            string? selecionado = CmbAcaoGlobal.SelectedItem as string;
            if (selecionado == null)
                return;

            if (selecionado == OpcaoDesabilitarTodos)
            {
                foreach (var mod in _todosOsMods)
                    mod.ModSelecionado = "Nenhum";
            }
            else
            {
                foreach (var mod in _todosOsMods.Where(m => m.OpcoesDisponiveis.Contains(selecionado)))
                    mod.ModSelecionado = selecionado;
            }

            AplicarFiltro();
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            SalvarConfiguracaoDeMods();
            DialogResult = true;
            Close();
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnAplicar_Click(object sender, RoutedEventArgs e) => SalvarConfiguracaoDeMods();

        private void SalvarConfiguracaoDeMods()
        {
            var ativos = _todosOsMods
                .Where(m => m.ModSelecionado != "Nenhum")
                .Select(m => new ModAtivo
                {
                    Arquivo = m.Arquivo,
                    Descricao = m.Descricao,
                    NomeDoPack = m.ModSelecionado,
                    CaminhoCompleto = Path.Combine(Paths.Main.Packs, m.ModSelecionado, m.Arquivo),
                    Categoria = m.Categoria switch
                    {
                        "BGM" => CategoriaMod.BGM,
                        "Video" => CategoriaMod.Video,
                        _ => CategoriaMod.Geral
                    }
                })
                .ToList();

            GerenciadorDeMods.Salvar(ativos);
            BtnAplicar.IsEnabled = false;
        }
    }
}
