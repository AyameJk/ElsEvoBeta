using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace ElsEvo
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
            BtnAplicarAcaoGlobal.Content = Idiomas.T("ModsAplicar");
            BtnMenu.Content = Idiomas.T("ModsMenu");
            TxtAplicarSemMod.Text = Idiomas.T("ModsAplicarSemMod");
            TxtMenuNovo.Text = Idiomas.T("ModsNovo");
            TxtMenuImportarPasta.Text = Idiomas.T("ModsImportarPasta");
            TxtMenuImportarZip.Text = Idiomas.T("ModsImportarZip");
            TxtMenuLocalArquivo.Text = Idiomas.T("ModsLocalArquivo");
            TxtMenuExportarZip.Text = Idiomas.T("ModsExportarZip");
            TxtMenuExcluirPack.Text = Idiomas.T("ModsExcluirPack");
            TxtAbaGeral.Text = Idiomas.T("AbaGeral");
            TxtAbaBgm.Text = Idiomas.T("AbaBgm");
            TxtAbaVideo.Text = Idiomas.T("AbaVideo");
            GridMods.Columns[0].Header = Idiomas.T("ModsArquivo");
            GridMods.Columns[1].Header = Idiomas.T("ModsDescricao");
            GridMods.Columns[2].Header = Idiomas.T("ModsMod");
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

                        if (!EhArquivoDeModValido(nomeArquivo))
                            continue;

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

                string nomePackParaDescricao = modSelecionado != "Nenhum" ? modSelecionado : packsComEsseArquivo.First();
                string pastaDoPackParaDescricao = Path.Combine(Paths.Main.Packs, nomePackParaDescricao);
                string? descricaoComOverride = BancoDeArquivos.DescricaoComOverridePack(pastaDoPackParaDescricao, nomeArquivo);
                string descricao = descricaoComOverride
                    ?? $"[{nomePackParaDescricao}] {Path.GetFileNameWithoutExtension(nomeArquivo)}";

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

        private static bool EhArquivoDeModValido(string nomeArquivo)
        {
            if (nomeArquivo.Equals(DublagensService.NomeArquivoDescricoesPack, StringComparison.OrdinalIgnoreCase))
                return false;

            return nomeArquivo.EndsWith(".kom", StringComparison.OrdinalIgnoreCase)
                || nomeArquivo.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase)
                || nomeArquivo.EndsWith(".avi", StringComparison.OrdinalIgnoreCase)
                || nomeArquivo.Equals("general.ess", StringComparison.OrdinalIgnoreCase);
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
            RegistroLog.Registrar("Criação de pack solicitada");

            string? nomeEscolhido = PromptSimples.PedirTexto(this, "Novo pack", "Nome do novo pack de mod:", "MeuMod");
            if (string.IsNullOrWhiteSpace(nomeEscolhido))
                return;

            string pastaPack = Path.Combine(Paths.Main.Packs, nomeEscolhido);
            Directory.CreateDirectory(pastaPack);

            JanelaConfirmacao.Mostrar(this,
                "Novo pack",
                $"Pack \"{nomeEscolhido}\" criado em:\n{pastaPack}\n\nAdicione arquivos .kom/.ogg/.avi dentro dessa pasta e importe-a com \"Importar pasta\".",
                TipoMensagem.Informacao);
        }

        private void MenuImportarPasta_Click(object sender, RoutedEventArgs e)
        {
            PopupMenu.IsOpen = false;
            RegistroLog.Registrar("Importação de pasta solicitada");

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
                .Where(f => EhArquivoDeModValido(Path.GetFileName(f)))
                .ToArray();

            if (arquivos.Length == 0)
            {
                JanelaConfirmacao.Mostrar(this,
                    "Importar pasta",
                    "Nenhum arquivo .kom, .ogg, .avi ou general.ess encontrado nessa pasta.",
                    TipoMensagem.Aviso);
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

                string? descricaoComOverride = BancoDeArquivos.DescricaoComOverridePack(pastaDestino, nomeArquivo);
                string descricao = descricaoComOverride
                    ?? $"[{nomePack}] {Path.GetFileNameWithoutExtension(nomeArquivo)}";

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
            RegistroLog.Registrar("Importação de ZIP solicitada");

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
                JanelaConfirmacao.Mostrar(this,
                    "Importar .zip",
                    $"Não foi possível extrair o zip:\n{ex.Message}\n\nSe o zip tiver senha, ainda não é suportado.",
                    TipoMensagem.Erro);
                return;
            }

            var arquivos = Directory.GetFiles(pastaDestino, "*.*", SearchOption.AllDirectories)
                .Where(f => EhArquivoDeModValido(Path.GetFileName(f)))
                .ToArray();

            for (int i = 0; i < arquivos.Length; i++)
            {
                string nomeArquivo = Path.GetFileName(arquivos[i]);
                string? descricaoComOverride = BancoDeArquivos.DescricaoComOverridePack(pastaDestino, nomeArquivo);
                string descricao = descricaoComOverride
                    ?? $"[{nomePack}] {Path.GetFileNameWithoutExtension(nomeArquivo)}";

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
            RegistroLog.Registrar("Exclusão de pack solicitada");

            string? nomePack = ObterPackSelecionado();

            if (nomePack == null)
            {
                JanelaConfirmacao.Mostrar(this,
                    "Excluir pack",
                    "Selecione um pack específico no combo do topo, ou uma linha com um pack definido, antes de excluir.",
                    TipoMensagem.Aviso);
                return;
            }

            bool confirmou = JanelaConfirmacao.Confirmar(this,
                "Excluir pack",
                $"Isso vai excluir o pack \"{nomePack}\" (e todos os arquivos dele) permanentemente. Continuar?",
                TipoMensagem.Aviso);

            if (!confirmou)
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

                JanelaConfirmacao.Mostrar(this,
                    "Excluir pack",
                    $"Pack \"{nomePack}\" excluído.",
                    TipoMensagem.Sucesso);
            }
            catch (Exception ex)
            {
                JanelaConfirmacao.Mostrar(this,
                    "Excluir pack",
                    $"Não foi possível excluir o pack:\n{ex.Message}",
                    TipoMensagem.Erro);
            }
        }

        private string? ObterPackSelecionado()
        {
            if (CmbAcaoGlobal.SelectedItem is string selecionadoGlobal && selecionadoGlobal != OpcaoDesabilitarTodos)
                return selecionadoGlobal;

            if (GridMods.SelectedItem is ModItem itemSelecionado && itemSelecionado.ModSelecionado != "Nenhum")
                return itemSelecionado.ModSelecionado;

            return null;
        }

        private string? ObterPastaPackSelecionado()
        {
            string? nomePack = ObterPackSelecionado();
            if (string.IsNullOrWhiteSpace(nomePack))
                return null;

            string pastaPack = Path.Combine(Paths.Main.Packs, nomePack);
            return Directory.Exists(pastaPack) ? pastaPack : null;
        }

        private void MenuAbrirNoExplorer_Click(object sender, RoutedEventArgs e)
        {
            PopupMenu.IsOpen = false;
            RegistroLog.Registrar("Local do arquivo solicitado");

            string? caminho = null;
            if (GridMods.SelectedItem is ModItem itemSelecionado && File.Exists(itemSelecionado.CaminhoCompleto))
                caminho = itemSelecionado.CaminhoCompleto;
            else if (ObterPastaPackSelecionado() is string pastaPack)
                caminho = pastaPack;
            else
                caminho = Paths.Main.Packs;

            Directory.CreateDirectory(caminho);

            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = File.Exists(caminho) ? $"/select,\"{caminho}\"" : $"\"{caminho}\"",
                UseShellExecute = true
            });
        }

        private void MenuExportarZip_Click(object sender, RoutedEventArgs e)
        {
            PopupMenu.IsOpen = false;
            RegistroLog.Registrar("Exportação de ZIP solicitada");

            string? pastaPack = ObterPastaPackSelecionado();
            string? nomePack = ObterPackSelecionado();
            if (pastaPack == null || nomePack == null)
            {
                JanelaConfirmacao.Mostrar(this,
                    "Exportar para .zip",
                    "Selecione um pack específico antes de exportar para .zip.",
                    TipoMensagem.Aviso);
                return;
            }

            var dialogo = new SaveFileDialog
            {
                Title = "Exportar pack para .zip",
                Filter = "Arquivo ZIP (*.zip)|*.zip",
                FileName = nomePack + ".zip",
                AddExtension = true,
                OverwritePrompt = true
            };

            if (dialogo.ShowDialog() != true)
                return;

            try
            {
                System.IO.Compression.ZipFile.CreateFromDirectory(
                    pastaPack, dialogo.FileName, System.IO.Compression.CompressionLevel.Optimal, includeBaseDirectory: false);

                JanelaConfirmacao.Mostrar(this,
                    "Exportar para .zip",
                    $"Pack exportado com sucesso para:\n{dialogo.FileName}",
                    TipoMensagem.Sucesso);
            }
            catch (Exception ex)
            {
                JanelaConfirmacao.Mostrar(this,
                    "Exportar para .zip",
                    $"Não foi possível exportar o pack:\n{ex.Message}",
                    TipoMensagem.Erro);
            }
        }

        private void BtnAplicarAcaoGlobal_Click(object sender, RoutedEventArgs e)
        {
            string? selecionado = CmbAcaoGlobal.SelectedItem as string;
            if (selecionado == null)
                return;

            RegistroLog.Registrar("Ação global de mods aplicada", selecionado);

            if (selecionado == OpcaoDesabilitarTodos)
            {
                foreach (var mod in _todosOsMods)
                    mod.ModSelecionado = "Nenhum";
            }
            else
            {
                bool somenteSemMod = ChkAplicarSomenteSemMod.IsChecked == true;

                var candidatos = _todosOsMods.Where(m => m.OpcoesDisponiveis.Contains(selecionado));
                if (somenteSemMod)
                    candidatos = candidatos.Where(m => m.ModSelecionado == "Nenhum");

                foreach (var mod in candidatos.ToList())
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
            RegistroLog.Registrar("Configuração de mods salva");
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
