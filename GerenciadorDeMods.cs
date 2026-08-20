using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ElsEvo
{
    public enum CategoriaMod
    {
        Geral,
        BGM,
        Video
    }

    public class ModAtivo
    {
        public string Arquivo { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public string NomeDoPack { get; set; } = string.Empty;
        public string CaminhoCompleto { get; set; } = string.Empty;
        public CategoriaMod Categoria { get; set; } = CategoriaMod.Geral;
    }

    public static class GerenciadorDeMods
    {
        public static List<ModAtivo> Carregar()
        {
            try
            {
                if (!File.Exists(Paths.UserMods))
                    return new List<ModAtivo>();

                string json = File.ReadAllText(Paths.UserMods);
                return JsonSerializer.Deserialize<List<ModAtivo>>(json) ?? new List<ModAtivo>();
            }
            catch
            {
                return new List<ModAtivo>();
            }
        }

        public static void Salvar(List<ModAtivo> modsAtivos)
        {
            Directory.CreateDirectory(Paths.LocalApplicationData);

            var opcoes = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(modsAtivos, opcoes);
            File.WriteAllText(Paths.UserMods, json);
        }
    }
}
