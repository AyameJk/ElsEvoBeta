using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ElsEvo
{
    public static class Paths
    {
        public static class Elsword
        {
            public static string Root => Properties.Settings.Default.ElswordDirectory;

            public static string Data => CriarSeValido(Path.Combine(Root, "data"));

            public static string ClientExe => Path.Combine(Data, "x2.exe");

            public static string LauncherExe => Path.Combine(Root, "elsword.exe");

            public static string Backup => CriarSeValido(Path.Combine(Root, "backup"));
            public static string Media => CriarSeValido(Path.Combine(Data, "media"));
            public static string Movie => CriarSeValido(Path.Combine(Data, "movie"));
            public static string Music => CriarSeValido(Path.Combine(Data, "music"));

            private static string[] ArquivosDeLog => new[]
            {
                Path.Combine(Data, "Crash_ScreenShot.jpg"),
                Path.Combine(Data, "log.htm")
            };

            public static bool IsValidElswordDir(string dir)
            {
                if (string.IsNullOrWhiteSpace(dir))
                    return false;

                string exe = Path.Combine(dir, "elsword.exe");
                string dataDir = Path.Combine(dir, "data");
                return File.Exists(exe) && Directory.Exists(dataDir);
            }

            public static void BlockLogs()
            {
                foreach (string arquivo in ArquivosDeLog)
                {
                    if (Directory.Exists(arquivo))
                        continue;

                    if (File.Exists(arquivo))
                        File.Delete(arquivo);

                    var pasta = Directory.CreateDirectory(arquivo);
                    pasta.Attributes = FileAttributes.ReadOnly | FileAttributes.Hidden
                        | FileAttributes.System | FileAttributes.Directory;
                }
            }

            public static void UnblockLogs()
            {
                foreach (string arquivo in ArquivosDeLog)
                {
                    if (!Directory.Exists(arquivo))
                        continue;

                    try
                    {
                        var pasta = new DirectoryInfo(arquivo) { Attributes = FileAttributes.Directory };
                        pasta.Delete(recursive: true);
                    }
                    catch { }
                }
            }

            public static Process? RunClient()
            {
                if (!File.Exists(ClientExe))
                    return null;

                return Process.Start(new ProcessStartInfo
                {
                    FileName = ClientExe,
                    Arguments = " " + (Properties.Settings.Default.X2Args ?? string.Empty),
                    WorkingDirectory = Data,
                    UseShellExecute = true
                });
            }

            public static Process? RunLauncher()
            {
                if (!File.Exists(LauncherExe))
                    return null;

                return Process.Start(new ProcessStartInfo
                {
                    FileName = LauncherExe,
                    WorkingDirectory = Root,
                    UseShellExecute = true
                });
            }

            public static Process? GetClientProcess()
            {
                foreach (var nomeConhecido in new[] { "x2", "x2_dx11" })
                {
                    var processos = Process.GetProcessesByName(nomeConhecido);
                    if (processos.Length > 0)
                        return processos[0];
                }

                string raiz = Root;
                if (string.IsNullOrWhiteSpace(raiz))
                    return null;

                try
                {
                    foreach (var processo in Process.GetProcesses())
                    {
                        try
                        {
                            string? caminhoExe = processo.MainModule?.FileName;
                            if (string.IsNullOrEmpty(caminhoExe))
                                continue;

                            bool rodaDeDentroDaPasta = caminhoExe.StartsWith(raiz, StringComparison.OrdinalIgnoreCase);
                            bool naoEhOLauncher = !caminhoExe.Equals(LauncherExe, StringComparison.OrdinalIgnoreCase);

                            if (rodaDeDentroDaPasta && naoEhOLauncher)
                                return processo;
                        }
                        catch
                        {
                        }
                    }
                }
                catch { }

                return null;
            }

            private static string CriarSeValido(string caminho)
            {
                if (!IsValidElswordDir(Root))
                    return string.Empty;

                Directory.CreateDirectory(caminho);
                return caminho;
            }
        }

        public static class Main
        {
            public static string Cache
            {
                get
                {
                    string raizElsword = Elsword.Root;
                    string raizDisco = !string.IsNullOrEmpty(raizElsword)
                        ? Path.GetPathRoot(raizElsword)!
                        : AppDomain.CurrentDomain.BaseDirectory;

                    if (string.IsNullOrEmpty(raizDisco))
                        raizDisco = AppDomain.CurrentDomain.BaseDirectory;

                    string caminho = Path.Combine(raizDisco, "gPatcher cache");
                    Directory.CreateDirectory(caminho);
                    return caminho;
                }
            }

            public static string Packs
            {
                get
                {
                    string raiz = Elsword.Root;
                    string caminho = !string.IsNullOrWhiteSpace(raiz) && Directory.Exists(raiz)
                        ? Path.Combine(raiz, "cacheElsEvo")
                        : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cacheElsEvo");

                    Directory.CreateDirectory(caminho);
                    MigrarPacksAntigosSeNecessario(caminho);
                    return caminho;
                }
            }

            private static void MigrarPacksAntigosSeNecessario(string pastaNova)
            {
                try
                {
                    string baseDoApp = AppDomain.CurrentDomain.BaseDirectory;

                    foreach (var nomeAntigo in new[] { "cacheElsEvo", "packs" })
                    {
                        string pastaAntiga = Path.Combine(baseDoApp, nomeAntigo);

                        if (string.Equals(pastaAntiga, pastaNova, StringComparison.OrdinalIgnoreCase))
                            continue;

                        if (!Directory.Exists(pastaAntiga))
                            continue;

                        var subpastasAntigas = Directory.GetDirectories(pastaAntiga);
                        if (subpastasAntigas.Length == 0)
                            continue;

                        foreach (var pastaPack in subpastasAntigas)
                        {
                            string nomePack = Path.GetFileName(pastaPack);
                            string destino = Path.Combine(pastaNova, nomePack);

                            if (!Directory.Exists(destino))
                                Directory.Move(pastaPack, destino);
                        }

                        if (Directory.Exists(pastaAntiga) && !Directory.EnumerateFileSystemEntries(pastaAntiga).Any())
                            Directory.Delete(pastaAntiga);
                    }
                }
                catch
                {
                }
            }
        }

        public static string LocalApplicationData { get; } =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ElsEvo");

        public static string UserMods { get; } =
            Path.Combine(LocalApplicationData, "usrmods.json");
    }
}
