using System;
using System.IO;
using System.Text.Json;

namespace ElsEvo.Properties
{
    /// <summary>
    /// Substitui o "Settings.settings" do Visual Studio. Campos e defaults copiados
    /// fielmente do Settings.cs real do gPatcher (decompilado), pra manter compatibilidade
    /// de comportamento com o app original.
    /// </summary>
    public sealed class Settings
    {
        private static readonly Lazy<Settings> _instancia = new(Carregar);
        public static Settings Default => _instancia.Value;

        // Fica em %LocalAppData%\ElsEvo\ (não mais dentro da pasta de build!) — assim
        // sobrevive a "dotnet clean" e rebuilds, que apagam bin\/obj\ mas nunca essa pasta.
        private static string CaminhoArquivo =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ElsEvo", "ElsEvo_config.json");

        // ===== Idênticos ao Settings.cs original do gPatcher =====
        public bool TrayIconEnabled { get; set; } = true;
        public bool WebLoginNeeded { get; set; } = false;
        public bool CheckForProgramUpdates { get; set; } = true;
        public string ElswordDirectory { get; set; } = string.Empty;
        public bool UpgradeRequired { get; set; } = false;
        public string VersionInfoUrl { get; set; } = "https://gpatcher2.googlecode.com/svn/trunk/versionInfo.txt";
        public bool EnableBackgroundImages { get; set; } = true;
        public bool IsTrayIconFirstTime { get; set; } = true;
        public bool StartHidden { get; set; } = false;
        public string Culture { get; set; } = string.Empty;
        public bool ModsEnabled { get; set; } = true;
        public bool BlockLogs { get; set; } = false;
        public string X2Args { get; set; } = string.Empty;
        public bool SkipLauncher { get; set; } = false;

        // BUILD BETA: "Beta apenas" MARCADO por padrão — por padrão consulta o próprio
        // canal (repositório ElsEvoBeta) no AtualizacaoService.
        public bool IgnoreBetaReleases { get; set; } = false;

        // BUILD BETA: mostra o badge "BETA" no menu principal e na janela Sobre.
        public bool IsBetaRelease { get; set; } = true;

        public bool BetaFirstLaunch { get; set; } = true;

        // ===== Campos extras do ElsEvo (não existiam no gPatcher original) =====
        /// <summary>true = tema Claro, false = tema Escuro. Padrão de fábrica: Claro.</summary>
        public bool TemaClaro { get; set; } = true;
        public bool IniciarComWindows { get; set; } = true;
        public bool MinimizarParaBandeja { get; set; } = true;
        public string Idioma { get; set; } = "pt";

        private static Settings Carregar()
        {
            try
            {
                if (File.Exists(CaminhoArquivo))
                {
                    string json = File.ReadAllText(CaminhoArquivo);
                    var carregado = JsonSerializer.Deserialize<Settings>(json);
                    if (carregado != null)
                        return carregado;
                }
            }
            catch
            {
                // JSON corrompido -> cai pro padrão silenciosamente.
            }

            return new Settings();
        }

        /// <summary>Restaura todos os valores para o padrão de fábrica ("Limpar configurações").</summary>
        public void Reset()
        {
            var padrao = new Settings();

            TrayIconEnabled = padrao.TrayIconEnabled;
            WebLoginNeeded = padrao.WebLoginNeeded;
            ElswordDirectory = padrao.ElswordDirectory;
            UpgradeRequired = padrao.UpgradeRequired;
            EnableBackgroundImages = padrao.EnableBackgroundImages;
            IsTrayIconFirstTime = padrao.IsTrayIconFirstTime;
            StartHidden = padrao.StartHidden;
            Culture = padrao.Culture;
            ModsEnabled = padrao.ModsEnabled;
            BlockLogs = padrao.BlockLogs;
            X2Args = padrao.X2Args;
            SkipLauncher = padrao.SkipLauncher;
            BetaFirstLaunch = padrao.BetaFirstLaunch;
            TemaClaro = padrao.TemaClaro;
            IniciarComWindows = padrao.IniciarComWindows;
            MinimizarParaBandeja = padrao.MinimizarParaBandeja;
            Idioma = padrao.Idioma;
            IsBetaRelease = padrao.IsBetaRelease;
            IgnoreBetaReleases = padrao.IgnoreBetaReleases;
            CheckForProgramUpdates = padrao.CheckForProgramUpdates;
        }

        public void Save()
        {
            string? pasta = Path.GetDirectoryName(CaminhoArquivo);
            if (!string.IsNullOrEmpty(pasta))
                Directory.CreateDirectory(pasta);

            var opcoes = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(this, opcoes);
            File.WriteAllText(CaminhoArquivo, json);
        }
    }
}
