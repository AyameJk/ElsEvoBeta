using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ElsEvo
{
    /// <summary>Um arquivo conhecido do jogo (equivalente ao ElswordFile original).</summary>
    public class ArquivoConhecido
    {
        [JsonPropertyName("FileName")]
        public string FileName { get; set; } = string.Empty;

        [JsonPropertyName("Description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("Blocked")]
        public bool Blocked { get; set; }

        [JsonPropertyName("BlockedServers")]
        public string BlockedServers { get; set; } = string.Empty;
    }

    /// <summary>
    /// Réplica do ElswordFilesManager original: um dicionário com a descrição de cada
    /// arquivo conhecido do jogo (data079.kom -> "[Vozes] Aisha", etc.), extraído do
    /// recurso real Text.Data_ElswordFileInfo (397 arquivos catalogados).
    /// </summary>
    public static class BancoDeArquivos
    {
        private static readonly Lazy<Dictionary<string, ArquivoConhecido>> _porNome = new(Carregar);

        public static ArquivoConhecido? BuscarPorNome(string nomeArquivo)
        {
            return _porNome.Value.TryGetValue(nomeArquivo, out var arquivo) ? arquivo : null;
        }

        /// <summary>Categoria (Geral/BGM/Video) baseada só na extensão — igual ao SearchCriterias original.</summary>
        public static CategoriaMod CategoriaPorExtensao(string nomeArquivo)
        {
            string ext = Path.GetExtension(nomeArquivo).ToLowerInvariant();
            return ext switch
            {
                ".ogg" => CategoriaMod.BGM,
                ".avi" => CategoriaMod.Video,
                _ => CategoriaMod.Geral
            };
        }

        private static Dictionary<string, ArquivoConhecido> Carregar()
        {
            try
            {
                string caminho = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "elsword_files_db.json");
                if (!File.Exists(caminho))
                    return new Dictionary<string, ArquivoConhecido>(StringComparer.OrdinalIgnoreCase);

                string json = File.ReadAllText(caminho);
                var lista = JsonSerializer.Deserialize<List<ArquivoConhecido>>(json) ?? new List<ArquivoConhecido>();

                return lista.ToDictionary(a => a.FileName, StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                return new Dictionary<string, ArquivoConhecido>(StringComparer.OrdinalIgnoreCase);
            }
        }
    }
}
