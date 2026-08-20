using System;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;

namespace ElsEvo
{
    public static class CarregarImagem
    {
        private static string PastaAssets => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");

        public static BitmapImage? BuscarPorNomeBase(string nomeBase)
        {
            try
            {
                if (!Directory.Exists(PastaAssets))
                    return null;

                string? caminho = Directory.GetFiles(PastaAssets)
                    .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f)
                        .Equals(nomeBase, StringComparison.OrdinalIgnoreCase));

                if (caminho == null)
                    return null;

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(caminho, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();

                return bitmap;
            }
            catch
            {
                return null;
            }
        }
    }
}
