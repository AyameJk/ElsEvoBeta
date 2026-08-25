using System;
using System.IO;
using System.Text.Json;

namespace ElsEvo.Properties
{
    public sealed class Settings
    {
        private static readonly Lazy<Settings> _instancia = new(Carregar);
        public static Settings Default => _instancia.Value;

        private static string CaminhoArquivo =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            NomePastaDados, "ElsEvo_config.json");

        private static string NomePastaDados =>
    #if ELSEVO_BETA
            "ElsEvoBeta";
    #else
            "ElsEvo";
    #endif

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

        public bool IgnoreBetaReleases { get; set; } = false;

        public bool IsBetaRelease { get; set; } = true;

        public bool BetaFirstLaunch { get; set; } = true;

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
            }

            return new Settings();
        }

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
