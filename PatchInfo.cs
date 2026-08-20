using System.IO;

namespace ElsEvo
{
    public class PatchInfo
    {
        public string ArquivoModificado { get; }
        public string ArquivoTemporario { get; }
        public string ArquivoDestino { get; }
        public string? ArquivoBackup { get; }

        public PatchInfo(ModAtivo mod)
        {
            ArquivoModificado = mod.CaminhoCompleto;
            ArquivoTemporario = Path.Combine(Paths.Main.Cache, mod.Arquivo);

            switch (mod.Categoria)
            {
                case CategoriaMod.Video:
                    ArquivoDestino = Path.Combine(Paths.Elsword.Movie, mod.Arquivo);
                    ArquivoBackup = Path.Combine(Paths.Elsword.Backup, mod.Arquivo);
                    break;
                case CategoriaMod.BGM:
                    ArquivoDestino = Path.Combine(Paths.Elsword.Media, mod.Arquivo);
                    ArquivoBackup = null;
                    break;
                default:
                    ArquivoDestino = Path.Combine(Paths.Elsword.Data, mod.Arquivo);
                    ArquivoBackup = Path.Combine(Paths.Elsword.Backup, mod.Arquivo);
                    break;
            }
        }
    }
}
