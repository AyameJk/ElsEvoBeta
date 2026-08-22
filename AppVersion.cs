namespace ElsEvo
{
    /// <summary>
    /// Versão do app. Dois campos com propósitos DIFERENTES:
    ///   - Numero: versão "bonita" exibida na janela Sobre (ex.: "1.0").
    ///   - VersaoParaAtualizacao: versão usada pelo AtualizacaoService pra comparar com o
    ///     version.json remoto. SEMPRE com 3 dígitos (Major.Minor.Build), nunca 1 ou 2 —
    ///     "1.0" e "1.0.0" NÃO são consideradas iguais pelo Version.TryParse (o primeiro
    ///     vira Build = -1, o segundo Build = 0), o que gera falso positivo de
    ///     "atualização disponível" mesmo estando na versão certa.
    /// </summary>
    public static class AppVersion
    {
        public const string Numero = "1.0";
        public const string VersaoParaAtualizacao = "1.0.502";
    }
}
