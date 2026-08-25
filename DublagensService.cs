using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace ElsEvo
{
    public sealed class DublagemDisponivel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("nome")]
        public string Nome { get; set; } = string.Empty;

        [JsonPropertyName("descricao")]
        public string Descricao { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("sha256")]
        public string Sha256 { get; set; } = string.Empty;
    }

    public static class DublagensService
    {
        private const string UrlManifesto =
            "https://raw.githubusercontent.com/AyameJk/ElsEvoDublagens/main/manifest.json";

        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromMinutes(30) };

        public static async Task<List<DublagemDisponivel>> ListarAsync(CancellationToken cancelamento = default)
        {
            string url = $"{UrlManifesto}?t={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
            string json = await Http.GetStringAsync(url, cancelamento);
            return JsonSerializer.Deserialize<List<DublagemDisponivel>>(json) ?? new List<DublagemDisponivel>();
        }

        public static async Task BaixarEInstalarAsync(
            DublagemDisponivel dublagem,
            IProgress<int>? progresso = null,
            IProgress<string>? status = null,
            CancellationToken cancelamento = default)
        {
            if (!Uri.TryCreate(dublagem.Url, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            {
                throw new InvalidOperationException("A URL da dublagem é inválida.");
            }

            string nomeSeguro = SanitizarNome(dublagem.Id);
            string pastaPack = Path.Combine(Paths.Main.Packs, nomeSeguro);
            string caminhoZip = Path.Combine(Paths.Main.Cache, nomeSeguro + ".zip");

            Directory.CreateDirectory(pastaPack);
            Directory.CreateDirectory(Paths.Main.Cache);
            status?.Report("Conectando ao servidor...");

            using var resposta = await Http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancelamento);
            resposta.EnsureSuccessStatusCode();

            long? tamanhoTotal = resposta.Content.Headers.ContentLength;
            await using var origem = await resposta.Content.ReadAsStreamAsync(cancelamento);
            await using var destino = File.Create(caminhoZip);
            var buffer = new byte[81920];
            long totalLido = 0;
            int lido;

            while ((lido = await origem.ReadAsync(buffer.AsMemory(), cancelamento)) > 0)
            {
                await destino.WriteAsync(buffer.AsMemory(0, lido), cancelamento);
                totalLido += lido;

                if (tamanhoTotal is > 0)
                    progresso?.Report((int)(totalLido * 100 / tamanhoTotal.Value));
            }

            await destino.FlushAsync(cancelamento);
            destino.Close();

            if (!string.IsNullOrWhiteSpace(dublagem.Sha256))
            {
                status?.Report("Verificando arquivo...");
                string hash = await CalcularSha256Async(caminhoZip, cancelamento);
                if (!hash.Equals(dublagem.Sha256.Trim(), StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("A verificação da dublagem falhou: o SHA-256 não confere.");
            }

            status?.Report("Extraindo dublagem...");
            await Task.Run(() => ExtrairComSeguranca(caminhoZip, pastaPack), cancelamento);
            File.Delete(caminhoZip);
            progresso?.Report(100);
            status?.Report("Dublagem instalada com sucesso.");
        }

        private static async Task<string> CalcularSha256Async(string caminho, CancellationToken cancelamento)
        {
            await using var stream = File.OpenRead(caminho);
            using var sha256 = SHA256.Create();
            byte[] hash = await sha256.ComputeHashAsync(stream, cancelamento);
            return Convert.ToHexString(hash);
        }

        private static void ExtrairComSeguranca(string caminhoZip, string pastaDestino)
        {
            string raiz = Path.GetFullPath(pastaDestino + Path.DirectorySeparatorChar);
            using var arquivoZip = ZipFile.OpenRead(caminhoZip);

            foreach (var entrada in arquivoZip.Entries)
            {
                string destino = Path.GetFullPath(Path.Combine(pastaDestino, entrada.FullName));
                if (!destino.StartsWith(raiz, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("O ZIP contém um caminho inválido.");

                if (string.IsNullOrEmpty(entrada.Name))
                {
                    Directory.CreateDirectory(destino);
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(destino)!);
                entrada.ExtractToFile(destino, overwrite: true);
            }
        }

        private static string SanitizarNome(string nome)
        {
            string limpo = new(nome.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_').ToArray());
            return string.IsNullOrWhiteSpace(limpo) ? "Dublagem" : limpo;
        }
    }
}
