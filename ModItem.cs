using System.Collections.Generic;
using System.ComponentModel;

namespace ElsEvo
{
    public class ModItem : INotifyPropertyChanged
    {
        private string _modSelecionado = string.Empty;

        public string Arquivo { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public string Categoria { get; set; } = "Geral";
        public string CaminhoCompleto { get; set; } = string.Empty;
        public List<string> OpcoesDisponiveis { get; set; } = new() { "Nenhum" };

        public string ModSelecionado
        {
            get => _modSelecionado;
            set
            {
                if (_modSelecionado != value)
                {
                    _modSelecionado = value;
                    OnPropertyChanged(nameof(ModSelecionado));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string nome) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nome));
    }
}
