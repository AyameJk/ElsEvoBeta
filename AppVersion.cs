namespace ElsEvo
{
    public static class AppVersion
    {
        public const string VersaoParaAtualizacao = "1.1.047";

        public static string Numero
        {
            get
            {
                if (System.Version.TryParse(VersaoParaAtualizacao, out var versao))
                    return $"{versao.Major}.{versao.Minor}.0";

                return VersaoParaAtualizacao;
            }
        }
    }
}
