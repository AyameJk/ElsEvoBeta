using System;
using System.Windows;
using WinForms = System.Windows.Forms;

namespace ElsEvo
{
    public class GerenciadorBandeja : IDisposable
    {
        private readonly WinForms.NotifyIcon _icone;
        private readonly Window _janela;

        public GerenciadorBandeja(Window janela, string caminhoIcone)
        {
            _janela = janela;

            _icone = new WinForms.NotifyIcon
            {
                Icon = System.Drawing.Icon.ExtractAssociatedIcon(caminhoIcone)
                       ?? System.Drawing.SystemIcons.Application,
                Visible = false,
                Text = "ElsEvo"
            };

            _icone.DoubleClick += (_, _) => Restaurar();

            var menu = new WinForms.ContextMenuStrip();
            menu.Items.Add("Abrir", null, (_, _) => Restaurar());
            menu.Items.Add(new WinForms.ToolStripSeparator());
            menu.Items.Add("Sair", null, (_, _) => Application.Current.Shutdown());
            _icone.ContextMenuStrip = menu;
        }

        public void Mostrar() => _icone.Visible = true;

        public void Esconder() => _icone.Visible = false;

        private void Restaurar()
        {
            _janela.Show();
            _janela.WindowState = WindowState.Normal;
            _janela.Activate();
            Esconder();
        }

        public void Dispose()
        {
            _icone.Visible = false;
            _icone.Dispose();
        }
    }
}
