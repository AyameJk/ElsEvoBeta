using System;
using System.IO;

namespace ElsEvo
{
    public static class RegistroLog
    {
        private static readonly object Bloqueio = new();

        public static void Registrar(string evento, string? detalhes = null)
        {
            try
            {
                Directory.CreateDirectory(Paths.LocalApplicationData);
                string caminho = Path.Combine(Paths.LocalApplicationData, "app-log.txt");
                string linha = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {evento}";
                if (!string.IsNullOrWhiteSpace(detalhes))
                    linha += $" | {detalhes}";

                lock (Bloqueio)
                    File.AppendAllText(caminho, linha + Environment.NewLine);
            }
            catch
            {
            }
        }

        public static void Erro(string evento, Exception ex) =>
            Registrar(evento, $"{ex.GetType().Name}: {ex.Message}");
    }
}
