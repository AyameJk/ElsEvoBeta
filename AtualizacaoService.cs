using System;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace ElsEvo
{
    public class InfoVersao
    {
        [JsonPropertyName("versao")]
        public string Versao { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("notas")]
        public string Notas { get; set; } = string.Empty;
    }

    public class AtualizacaoDisponivel
    {
        public string VersaoNova { get; set; } = string.Empty;
        public string UrlInstalador { get; set; } = string.Empty;
        public string Notas { get; set; } = string.Empty;

        public bool EhCanalBeta { get; set; }
    }

    public static class AtualizacaoService
    {
        private const string UrlVersionJsonEstavel =
            "https://raw.githubusercontent.com/AyameJk/ElsEvo/main/version.json";

        private const string UrlVersionJsonBeta =
            "https://raw.githubusercontent.com/AyameJk/ElsEvoBeta/main/version.json";

        private static readonly HttpClient _http = new()
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        public static async Task<AtualizacaoDisponivel?> VerificarAsync()
        {
            try
            {
                bool buscarBeta = !Properties.Settings.Default.IgnoreBetaReleases;
                string urlManifesto = buscarBeta ? UrlVersionJsonBeta : UrlVersionJsonEstavel;

                string url = $"{urlManifesto}?t={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

                string json = await _http.GetStringAsync(url);
                var info = JsonSerializer.Deserialize<InfoVersao>(json);

                if (info == null || string.IsNullOrWhiteSpace(info.Versao) || string.IsNullOrWhiteSpace(info.Url))
                    return null;

                if (buscarBeta && !VersaoEhMaisNova(info.Versao, AppVersion.VersaoParaAtualizacao))
                    return null;

                return new AtualizacaoDisponivel
                {
                    VersaoNova = info.Versao,
                    UrlInstalador = info.Url,
                    Notas = info.Notas,
                    EhCanalBeta = buscarBeta
                };
            }
            catch
            {
                return null;
            }
        }

        private static bool VersaoEhMaisNova(string versaoRemota, string versaoAtual)
        {
            if (Version.TryParse(versaoRemota, out var vRemota) && Version.TryParse(versaoAtual, out var vAtual))
                return vRemota > vAtual;

            return !string.Equals(versaoRemota, versaoAtual, StringComparison.OrdinalIgnoreCase);
        }
    }
}
