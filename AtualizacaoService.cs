using System;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace ElsEvo
{
    /// <summary>
    /// Conteúdo do version.json — um objeto só por repositório (a separação estável/beta
    /// é por repositório, não por chave dentro do mesmo arquivo).
    /// </summary>
    public class InfoVersao
    {
        [JsonPropertyName("versao")]
        public string Versao { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("notas")]
        public string Notas { get; set; } = string.Empty;
    }

    /// <summary>Resultado de uma checagem que encontrou uma versão mais nova disponível.</summary>
    public class AtualizacaoDisponivel
    {
        public string VersaoNova { get; set; } = string.Empty;
        public string UrlInstalador { get; set; } = string.Empty;
        public string Notas { get; set; } = string.Empty;

        /// <summary>true quando essa atualização veio do canal BETA — usado pra avisar o
        /// usuário antes de instalar por cima de uma instalação estável (ou vice-versa).</summary>
        public bool EhCanalBeta { get; set; }
    }

    /// <summary>
    /// Verifica se existe uma versão mais nova do ElsEvo publicada. Cada canal é um
    /// REPOSITÓRIO GITHUB SEPARADO, cada um com seu próprio version.json na raiz:
    ///   - Canal beta (esta build):    repositório ElsEvoBeta
    ///   - Canal estável (build irmã): repositório ElsEvo
    ///
    /// IMPORTANTE — capitalização: os nomes reais dos repositórios são "ElsEvo" e
    /// "ElsEvoBeta" (exatamente essa capitalização, não "ElsEvo"/"ElsEvoBeta" com maiúsculas diferentes). A página
    /// normal do GitHub redireciona ignorando maiúscula/minúscula, mas o
    /// raw.githubusercontent.com pode não fazer esse redirecionamento de forma confiável
    /// — usar a capitalização errada aqui causa falha SILENCIOSA na checagem (a exceção
    /// cai no catch genérico do VerificarAsync e o app simplesmente nunca acusa
    /// atualização nenhuma, sem erro visível nenhum).
    ///
    /// "Beta apenas" MARCADO -> consulta o canal beta (repositório ElsEvoBeta — é o que
    /// esta build já é, por padrão). "Beta apenas" DESMARCADO -> consulta o canal estável
    /// (repositório ElsEvo) e oferece baixar/instalar a build estável por cima desta
    /// instalação beta.
    /// </summary>
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

        /// <summary>
        /// Retorna os dados da atualização se houver uma versão mais nova que a atual no
        /// canal certo (estável ou beta, conforme "Beta apenas" nas Configurações).
        /// Retorna null se já estiver na versão mais recente, ou se a checagem falhar por
        /// qualquer motivo (sem internet, GitHub fora do ar, JSON mudou de formato,
        /// capitalização errada de URL, etc.) — checagem de atualização NUNCA deve travar
        /// ou incomodar o usuário ao abrir o app, então qualquer erro aqui é silencioso.
        /// </summary>
        public static async Task<AtualizacaoDisponivel?> VerificarAsync()
        {
            try
            {
                bool buscarBeta = !Properties.Settings.Default.IgnoreBetaReleases;
                string urlManifesto = buscarBeta ? UrlVersionJsonBeta : UrlVersionJsonEstavel;

                // Cache-busting: o raw.githubusercontent.com às vezes serve uma cópia em
                // cache por alguns minutos depois do push — o parâmetro garante que a
                // gente sempre pega a versão mais recente de verdade.
                string url = $"{urlManifesto}?t={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

                string json = await _http.GetStringAsync(url);
                var info = JsonSerializer.Deserialize<InfoVersao>(json);

                if (info == null || string.IsNullOrWhiteSpace(info.Versao) || string.IsNullOrWhiteSpace(info.Url))
                    return null;

                // A comparação numérica de versão só faz sentido DENTRO do mesmo canal —
                // os dois canais numeram suas rodadas de forma independente (ex.: beta
                // pode estar em "1.0.450" enquanto o estável está em "1.0.4"), então "maior
                // que" não tem significado nenhum entre eles. Quando o usuário desmarca
                // "Beta apenas" e o app passa a consultar o canal ESTÁVEL, é sempre uma
                // TROCA DE CANAL (instalar a build estável por cima da beta), não uma
                // sequência — por isso sempre oferece, mesmo que o número pareça "menor".
                // Só dentro do próprio canal (buscarBeta == true) a comparação numérica
                // continua valendo, pra não ficar oferecendo a mesma versão repetidamente.
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

        /// <summary>
        /// Compara duas versões no formato "1.0.XXX" numericamente (Major.Minor.Build).
        ///
        /// CUIDADO: "1.0" e "1.0.0" NÃO são iguais pro Version.TryParse ("1.0" vira
        /// Build = -1, "1.0.0" vira Build = 0) — isso pode gerar "atualização disponível"
        /// falsa mesmo estando na versão certa. Por isso AppVersion.VersaoParaAtualizacao
        /// e o campo "versao" do version.json remoto SEMPRE precisam ter a mesma
        /// quantidade de dígitos entre si (3 números: Major.Minor.Build — ex.: "1.0.023",
        /// nunca "1.0.23" nem "1.0").
        /// </summary>
        private static bool VersaoEhMaisNova(string versaoRemota, string versaoAtual)
        {
            if (Version.TryParse(versaoRemota, out var vRemota) && Version.TryParse(versaoAtual, out var vAtual))
                return vRemota > vAtual;

            // Formato inesperado (não parseável como Version) — fallback simples: só
            // considera "mais nova" se for literalmente diferente da atual.
            return !string.Equals(versaoRemota, versaoAtual, StringComparison.OrdinalIgnoreCase);
        }
    }
}
