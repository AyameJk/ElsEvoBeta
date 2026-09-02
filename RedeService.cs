using System;
using System.Net;
using System.Net.Http;

namespace ElsEvo
{
    public static class RedeService
    {
        // Cria um HttpClient já configurado com o timeout e o proxy definidos em
        // Configurações -> Rede. Chamado sempre que o app precisa fazer uma requisição
        // (checar atualização, listar/baixar dublagens), em vez de reaproveitar uma
        // instância estática fixa -- assim, se o usuário mudar o proxy ou o timeout,
        // a mudança já vale na próxima requisição, sem precisar reiniciar o app.
        public static HttpClient CriarHttpClient(TimeSpan timeout)
        {
            var handler = new HttpClientHandler();
            var cfg = Properties.Settings.Default;

            if (cfg.ProxyHabilitado && !string.IsNullOrWhiteSpace(cfg.ProxyEndereco))
            {
                try
                {
                    var proxy = new WebProxy(cfg.ProxyEndereco, cfg.ProxyPorta);

                    if (!string.IsNullOrWhiteSpace(cfg.ProxyUsuario))
                        proxy.Credentials = new NetworkCredential(cfg.ProxyUsuario, cfg.ProxySenha ?? string.Empty);

                    handler.Proxy = proxy;
                    handler.UseProxy = true;
                }
                catch
                {
                    // Proxy mal configurado -- segue sem proxy em vez de travar o app.
                }
            }

            return new HttpClient(handler) { Timeout = timeout };
        }

        // Verifica se a conexão de internet ativa está marcada pelo Windows como "limitada"
        // (rede medida -- ex.: hotspot do celular, plano de dados com limite). Essa checagem
        // só lê uma classificação que o próprio Windows já mantém localmente para qualquer
        // app que perguntar; nenhuma informação sai da máquina do usuário.
        public static bool RedeEhLimitada()
        {
            try
            {
                var perfil = Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile();
                if (perfil == null)
                    return false;

                var custo = perfil.GetConnectionCost();
                return custo.NetworkCostType == Windows.Networking.Connectivity.NetworkCostType.Fixed
                    || custo.NetworkCostType == Windows.Networking.Connectivity.NetworkCostType.Variable;
            }
            catch
            {
                // Se a checagem falhar por qualquer motivo (API indisponível, versão antiga
                // do Windows, etc.), assume que a rede não é limitada em vez de bloquear ou
                // incomodar o usuário à toa.
                return false;
            }
        }
    }
}
