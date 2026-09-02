using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using SharpCompress.Archives;

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

        [JsonPropertyName("arquivo")]
        public string Arquivo { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("sha256")]
        public string Sha256 { get; set; } = string.Empty;

        [JsonPropertyName("tamanhoBytes")]
        [JsonConverter(typeof(LongTolerante))]
        public long? TamanhoBytes { get; set; }

        [JsonPropertyName("descricoes")]
        public Dictionary<string, string>? Descricoes { get; set; }
    }

    internal sealed class LongTolerante : JsonConverter<long?>
    {
        public override long? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt64(out long valorNumero))
                return valorNumero;

            if (reader.TokenType == JsonTokenType.String)
            {
                string? texto = reader.GetString();
                if (!string.IsNullOrWhiteSpace(texto) && long.TryParse(texto, out long valorTexto))
                    return valorTexto;
            }

            return null;
        }

        public override void Write(Utf8JsonWriter writer, long? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
                writer.WriteNumberValue(value.Value);
            else
                writer.WriteNullValue();
        }
    }

    public readonly struct ProgressoDownload
    {
        public int Percentual { get; init; }
        public long BytesRecebidos { get; init; }
        public long? BytesTotal { get; init; }
        public double VelocidadeKBps { get; init; }
    }

    public static class DublagensService
    {
        private const string UrlManifesto =
            "https://raw.githubusercontent.com/AyameJk/ElsEvoDublagens/main/manifest.json";

        private const int TamanhoBufferIO = 1024 * 1024;

        private static readonly TimeSpan IntervaloMinimoProgresso = TimeSpan.FromMilliseconds(200);

        private const string NomePastaTempExtracaoPacote = "Temp_Extract_Pacote";

        public const string NomeArquivoDescricoesPack = "_descricoes_pack.json";

        public static async Task<List<DublagemDisponivel>> ListarAsync(
            CancellationToken cancelamento = default)
        {
            string url =
                $"{UrlManifesto}?t={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

            var timeout = TimeSpan.FromSeconds(Properties.Settings.Default.TimeoutVerificacaoAtualizacaoSegundos);
            using var http = RedeService.CriarHttpClient(timeout);

            string json = await http.GetStringAsync(url, cancelamento);

            using JsonDocument documento =
                JsonDocument.Parse(json);

            if (documento.RootElement.ValueKind ==
                JsonValueKind.Object)
            {
                DublagemDisponivel? dublagem =
                    JsonSerializer.Deserialize<DublagemDisponivel>(
                        json);

                return dublagem != null
                    ? new List<DublagemDisponivel> { dublagem }
                    : new List<DublagemDisponivel>();
            }

            if (documento.RootElement.ValueKind ==
                JsonValueKind.Array)
            {
                return JsonSerializer.Deserialize<
                           List<DublagemDisponivel>>(json)
                       ?? new List<DublagemDisponivel>();
            }

            throw new InvalidDataException(
                "O manifesto de dublagens possui um formato inválido.");
        }

        public static async Task BaixarEInstalarAsync(
            DublagemDisponivel dublagem,
            IProgress<ProgressoDownload>? progresso = null,
            IProgress<string>? status = null,
            CancellationToken cancelamento = default,
            CancellationToken? cancelamentoPausa = null)
        {
            if (dublagem == null)
                throw new ArgumentNullException(nameof(dublagem));

            if (string.IsNullOrWhiteSpace(dublagem.Url))
            {
                throw new InvalidOperationException(
                    "A dublagem não possui uma URL de download.");
            }

            if (!Uri.TryCreate(
                    dublagem.Url,
                    UriKind.Absolute,
                    out Uri? uri)
                || (uri.Scheme != Uri.UriSchemeHttps &&
                    uri.Scheme != Uri.UriSchemeHttp))
            {
                throw new InvalidOperationException(
                    "A URL da dublagem é inválida.");
            }

            string nomeSeguro =
                SanitizarNome(dublagem.Id);

            string pastaPack =
                Path.Combine(
                    Paths.Main.Packs,
                    nomeSeguro);

            Directory.CreateDirectory(pastaPack);
            Directory.CreateDirectory(Paths.Main.Cache);

            string extensao =
                ObterExtensao(dublagem.Arquivo);

            string caminhoArquivo =
                Path.Combine(
                    Paths.Main.Cache,
                    nomeSeguro + extensao);

            var cfg = Properties.Settings.Default;
            int tentativasMaximas = Math.Max(1, cfg.TentativasAutomaticas);

            try
            {
                for (int tentativa = 1; tentativa <= tentativasMaximas; tentativa++)
                {
                    cancelamento.ThrowIfCancellationRequested();

                    try
                    {
                        status?.Report(
                            $"Baixando {dublagem.Arquivo}...");

                        var timeoutDownload = TimeSpan.FromMinutes(cfg.TimeoutDownloadMinutos);
                        using var http = RedeService.CriarHttpClient(timeoutDownload);

                        await BaixarArquivoAsync(
                            http,
                            uri,
                            caminhoArquivo,
                            progresso,
                            cancelamento);

                        if (!string.IsNullOrWhiteSpace(
                                dublagem.Sha256))
                        {
                            status?.Report(
                                "Verificando arquivo...");

                            string hash =
                                await CalcularSha256Async(
                                    caminhoArquivo,
                                    cancelamento);

                            if (!hash.Equals(
                                    dublagem.Sha256.Trim(),
                                    StringComparison.OrdinalIgnoreCase))
                            {
                                throw new InvalidDataException(
                                    "A verificação da dublagem falhou: " +
                                    "o SHA-256 não confere.");
                            }
                        }

                        status?.Report(
                            $"Extraindo {extensao.ToUpperInvariant()}...");

                        long tamanhoPacoteCompactado = new FileInfo(caminhoArquivo).Length;

                        await ExtrairComSegurancaAsync(
                            caminhoArquivo,
                            pastaPack,
                            cancelamento);

                        await GravarDescricoesDoPackAsync(pastaPack, dublagem.Descricoes, cancelamento);

                        status?.Report(
                            "Dublagem instalada com sucesso.");

                        return;
                    }
                    catch (Exception ex) when (
                        tentativa < tentativasMaximas &&
                        EhFalhaTransitoria(ex) &&
                        !cancelamento.IsCancellationRequested)
                    {
                        RegistroLog.Erro(
                            $"Falha na tentativa {tentativa}/{tentativasMaximas} ao baixar dublagem {dublagem.Id}", ex);

                        status?.Report(
                            $"Falha na tentativa {tentativa}/{tentativasMaximas}, tentando de novo...");

                        await Task.Delay(TimeSpan.FromSeconds(2 * tentativa), cancelamento);
                    }
                }
            }
            finally
            {
                bool foiPausado = cancelamentoPausa?.IsCancellationRequested == true;

                if (!foiPausado)
                {
                    if (File.Exists(caminhoArquivo))
                    {
                        try
                        {
                            File.Delete(caminhoArquivo);
                        }
                        catch
                        {
                        }
                    }

                    string caminhoValidacaoFinal = caminhoArquivo + ".validacao";
                    if (File.Exists(caminhoValidacaoFinal))
                    {
                        try
                        {
                            File.Delete(caminhoValidacaoFinal);
                        }
                        catch
                        {
                        }
                    }
                }
            }
        }

        private static bool EhFalhaTransitoria(Exception ex) =>
            ex is HttpRequestException or IOException or TaskCanceledException;

        private static async Task BaixarArquivoAsync(
            HttpClient http,
            Uri uri,
            string caminhoArquivo,
            IProgress<ProgressoDownload>? progresso,
            CancellationToken cancelamento)
        {
            string caminhoValidacao = caminhoArquivo + ".validacao";

            long bytesJaBaixados = 0;
            string? validadorParaIfRange = null;

            if (File.Exists(caminhoArquivo))
            {
                long tamanhoParcial = new FileInfo(caminhoArquivo).Length;

                if (tamanhoParcial > 0 && File.Exists(caminhoValidacao))
                {
                    string? validadorSalvo = null;
                    try
                    {
                        validadorSalvo = (await File.ReadAllTextAsync(caminhoValidacao, cancelamento)).Trim();
                    }
                    catch
                    {
                        validadorSalvo = null;
                    }

                    if (!string.IsNullOrWhiteSpace(validadorSalvo))
                    {
                        bytesJaBaixados = tamanhoParcial;
                        validadorParaIfRange = validadorSalvo;
                    }
                }

                if (bytesJaBaixados == 0)
                {
                    try { File.Delete(caminhoArquivo); } catch { }
                    try { if (File.Exists(caminhoValidacao)) File.Delete(caminhoValidacao); } catch { }
                }
            }

            using var requisicao = new HttpRequestMessage(HttpMethod.Get, uri);

            if (bytesJaBaixados > 0)
            {
                requisicao.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(bytesJaBaixados, null);

                if (validadorParaIfRange!.StartsWith("ETAG:", StringComparison.Ordinal))
                {
                    try
                    {
                        requisicao.Headers.IfRange = new System.Net.Http.Headers.RangeConditionHeaderValue(
                            System.Net.Http.Headers.EntityTagHeaderValue.Parse(validadorParaIfRange.Substring(5)));
                    }
                    catch
                    {
                        bytesJaBaixados = 0;
                        try { File.Delete(caminhoArquivo); } catch { }
                        try { if (File.Exists(caminhoValidacao)) File.Delete(caminhoValidacao); } catch { }
                    }
                }
                else if (validadorParaIfRange.StartsWith("DATA:", StringComparison.Ordinal)
                         && DateTimeOffset.TryParse(validadorParaIfRange.Substring(5), out var dataSalva))
                {
                    requisicao.Headers.IfRange = new System.Net.Http.Headers.RangeConditionHeaderValue(dataSalva);
                }
                else
                {
                    bytesJaBaixados = 0;
                    try { File.Delete(caminhoArquivo); } catch { }
                    try { if (File.Exists(caminhoValidacao)) File.Delete(caminhoValidacao); } catch { }
                }
            }

            using var resposta = await http.SendAsync(
                requisicao,
                HttpCompletionOption.ResponseHeadersRead,
                cancelamento);

            bool retomandoComSucesso = bytesJaBaixados > 0 &&
                resposta.StatusCode == System.Net.HttpStatusCode.PartialContent;

            if (bytesJaBaixados > 0 && !retomandoComSucesso)
                bytesJaBaixados = 0;

            resposta.EnsureSuccessStatusCode();

            string? validadorAtual = resposta.Headers.ETag != null
                ? "ETAG:" + resposta.Headers.ETag.ToString()
                : resposta.Content.Headers.LastModified is DateTimeOffset dataModificacao
                    ? "DATA:" + dataModificacao.ToString("O")
                    : null;

            if (validadorAtual != null)
            {
                try { await File.WriteAllTextAsync(caminhoValidacao, validadorAtual, cancelamento); }
                catch { }
            }
            else
            {
                try { if (File.Exists(caminhoValidacao)) File.Delete(caminhoValidacao); }
                catch { }
            }

            long? tamanhoRestante = resposta.Content.Headers.ContentLength;
            long? tamanhoTotal = retomandoComSucesso && tamanhoRestante is long restante
                ? bytesJaBaixados + restante
                : tamanhoRestante;

            await using var origem =
                await resposta.Content.ReadAsStreamAsync(
                    cancelamento);

            var modoAbertura = retomandoComSucesso ? FileMode.Append : FileMode.Create;

            await using var destino = new FileStream(
                caminhoArquivo,
                modoAbertura,
                FileAccess.Write,
                FileShare.None,
                TamanhoBufferIO,
                useAsync: true);

            var cfg = Properties.Settings.Default;
            long limiteBytesPorSegundo = cfg.LimitarVelocidadeDownload
                ? Math.Max(1, cfg.LimiteVelocidadeDownloadKBps) * 1024L
                : 0;

            byte[] buffer = new byte[TamanhoBufferIO];

            long totalLido = bytesJaBaixados;
            int lido;

            var cronometro = Stopwatch.StartNew();
            var cronometroProgresso = Stopwatch.StartNew();

            while ((lido = await origem.ReadAsync(
                       buffer.AsMemory(),
                       cancelamento)) > 0)
            {
                await destino.WriteAsync(
                    buffer.AsMemory(0, lido),
                    cancelamento);

                totalLido += lido;

                if (limiteBytesPorSegundo > 0)
                {
                    double segundosEsperados = (double)(totalLido - bytesJaBaixados) / limiteBytesPorSegundo;
                    double segundosDecorridos = cronometro.Elapsed.TotalSeconds;
                    double atraso = segundosEsperados - segundosDecorridos;

                    if (atraso > 0)
                        await Task.Delay(TimeSpan.FromSeconds(atraso), cancelamento);
                }

                bool ehUltimoBloco = tamanhoTotal is > 0 && totalLido >= tamanhoTotal.Value;
                if (progresso != null && (cronometroProgresso.Elapsed >= IntervaloMinimoProgresso || ehUltimoBloco))
                {
                    double elapsedSegundos = Math.Max(cronometro.Elapsed.TotalSeconds, 0.001);
                    double velocidadeKBps = ((totalLido - bytesJaBaixados) / 1024.0) / elapsedSegundos;

                    int percentual = tamanhoTotal is > 0
                        ? Math.Clamp((int)(totalLido * 100 / tamanhoTotal.Value), 0, 100)
                        : 0;

                    progresso.Report(new ProgressoDownload
                    {
                        Percentual = percentual,
                        BytesRecebidos = totalLido,
                        BytesTotal = tamanhoTotal,
                        VelocidadeKBps = velocidadeKBps
                    });

                    cronometroProgresso.Restart();
                }
            }

            await destino.FlushAsync(
                cancelamento);
        }

        private static string ObterExtensao(
            string arquivo)
        {
            if (string.IsNullOrWhiteSpace(arquivo))
            {
                return ".rar";
            }

            string extensao =
                Path.GetExtension(arquivo);

            if (extensao.Equals(
                    ".rar",
                    StringComparison.OrdinalIgnoreCase))
            {
                return ".rar";
            }

            if (extensao.Equals(
                    ".zip",
                    StringComparison.OrdinalIgnoreCase))
            {
                return ".zip";
            }

            throw new InvalidDataException(
                $"Formato de arquivo não suportado: {extensao}");
        }

        private static async Task<string> CalcularSha256Async(
            string caminho,
            CancellationToken cancelamento)
        {
            await using var stream = new FileStream(
                caminho,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                TamanhoBufferIO,
                useAsync: true);

            using var sha256 =
                SHA256.Create();

            byte[] hash =
                await sha256.ComputeHashAsync(
                    stream,
                    cancelamento);

            return Convert.ToHexString(hash);
        }

        private static async Task ExtrairComSegurancaAsync(
            string caminhoArquivo,
            string pastaDestino,
            CancellationToken cancelamento)
        {
            string pastaTemp = Path.Combine(Paths.Main.Cache, NomePastaTempExtracaoPacote);

            if (Directory.Exists(pastaTemp))
                Directory.Delete(pastaTemp, recursive: true);

            Directory.CreateDirectory(pastaTemp);

            try
            {
                await Task.Run(async () =>
                {
                    string raizTemp =
                        Path.GetFullPath(
                            pastaTemp +
                            Path.DirectorySeparatorChar);

                    using var arquivo =
                        ArchiveFactory.OpenArchive(
                            caminhoArquivo);

                    foreach (var entrada in arquivo.Entries)
                    {
                        cancelamento.ThrowIfCancellationRequested();

                        if (string.IsNullOrEmpty(
                                entrada.Key))
                        {
                            continue;
                        }

                        string destino =
                            Path.GetFullPath(
                                Path.Combine(
                                    pastaTemp,
                                    entrada.Key));

                        if (!destino.StartsWith(
                                raizTemp,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            throw new InvalidDataException(
                                "O arquivo compactado contém " +
                                "um caminho inválido.");
                        }

                        if (entrada.IsDirectory)
                        {
                            Directory.CreateDirectory(
                                destino);

                            continue;
                        }

                        string? diretorio =
                            Path.GetDirectoryName(
                                destino);

                        if (!string.IsNullOrEmpty(
                                diretorio))
                        {
                            Directory.CreateDirectory(
                                diretorio);
                        }

                        using Stream origem =
                            entrada.OpenEntryStream();

                        await using FileStream destinoStream = new(
                            destino,
                            FileMode.Create,
                            FileAccess.Write,
                            FileShare.None,
                            TamanhoBufferIO,
                            useAsync: true);

                        await origem.CopyToAsync(destinoStream, TamanhoBufferIO, cancelamento);
                    }
                }, cancelamento);

                Directory.CreateDirectory(pastaDestino);

                var arquivosExtraidos = Directory.GetFiles(pastaTemp, "*", SearchOption.AllDirectories);

                foreach (var caminhoOrigemArquivo in arquivosExtraidos)
                {
                    cancelamento.ThrowIfCancellationRequested();

                    string nomeArquivo = Path.GetFileName(caminhoOrigemArquivo);
                    if (string.IsNullOrEmpty(nomeArquivo))
                        continue;

                    string caminhoFinal = Path.Combine(pastaDestino, nomeArquivo);

                    if (File.Exists(caminhoFinal))
                        File.Delete(caminhoFinal);

                    File.Move(caminhoOrigemArquivo, caminhoFinal);
                }
            }
            finally
            {
                try
                {
                    if (Directory.Exists(pastaTemp))
                        Directory.Delete(pastaTemp, recursive: true);
                }
                catch (Exception ex)
                {
                    RegistroLog.Erro($"Falha ao limpar pasta temporária de extração {pastaTemp}", ex);
                }
            }
        }

        private static async Task GravarDescricoesDoPackAsync(
            string pastaPack,
            Dictionary<string, string>? descricoes,
            CancellationToken cancelamento)
        {
            string caminhoSidecar = Path.Combine(pastaPack, NomeArquivoDescricoesPack);

            if (descricoes == null || descricoes.Count == 0)
            {
                if (File.Exists(caminhoSidecar))
                {
                    try { File.Delete(caminhoSidecar); } catch { }
                }
                return;
            }

            var opcoes = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(descricoes, opcoes);

            await File.WriteAllTextAsync(caminhoSidecar, json, cancelamento);
        }

        public static Dictionary<string, string>? LerDescricoesDoPack(string pastaPack)
        {
            try
            {
                string caminhoSidecar = Path.Combine(pastaPack, NomeArquivoDescricoesPack);
                if (!File.Exists(caminhoSidecar))
                    return null;

                string json = File.ReadAllText(caminhoSidecar);
                return JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            }
            catch
            {
                return null;
            }
        }

        public static string FormatarTamanho(long bytes)
        {
            const double umGB = 1024.0 * 1024 * 1024;
            const double umMB = 1024.0 * 1024;
            const double umKB = 1024.0;

            if (bytes >= umGB)
                return $"{bytes / umGB:0.##} GB";

            if (bytes >= umMB)
                return $"{bytes / umMB:0} MB";

            return $"{bytes / umKB:0} KB";
        }

        private static string SanitizarNome(
            string nome)
        {
            string limpo = new(
                nome.Where(c =>
                    char.IsLetterOrDigit(c) ||
                    c is '-' or '_')
                .ToArray());

            return string.IsNullOrWhiteSpace(
                limpo)
                ? "Dublagem"
                : limpo;
        }
    }
}
