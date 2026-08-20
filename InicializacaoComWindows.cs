using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace ElsEvo
{
    public static class InicializacaoComWindows
    {
        private const string CaminhoChave = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string NomeValor = "ElsEvo";

        public static void Aplicar(bool habilitar)
        {
            try
            {
                using var chave = Registry.CurrentUser.OpenSubKey(CaminhoChave, writable: true);
                if (chave == null)
                    return;

                if (habilitar)
                {
                    string caminhoExe = Process.GetCurrentProcess().MainModule?.FileName
                                        ?? Environment.ProcessPath
                                        ?? string.Empty;

                    if (!string.IsNullOrEmpty(caminhoExe))
                        chave.SetValue(NomeValor, $"\"{caminhoExe}\"");
                }
                else
                {
                    chave.DeleteValue(NomeValor, throwOnMissingValue: false);
                }
            }
            catch
            {
            }
        }
    }
}
