using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ElsEvo
{
    public static class BarraTituloNativa
    {
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        public static void AplicarTema(Window janela, bool temaEscuro)
        {
            try
            {
                var helper = new WindowInteropHelper(janela);
                IntPtr hwnd = helper.Handle;
                if (hwnd == IntPtr.Zero)
                    return;

                int valor = temaEscuro ? 1 : 0;
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref valor, sizeof(int));
            }
            catch
            {
            }
        }
    }
}
