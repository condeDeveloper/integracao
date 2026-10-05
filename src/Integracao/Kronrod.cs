namespace Conde.Integracao;

/// <summary>
/// A estimativa de erro EMBUTIDA: como saber que a resposta esta certa sem
/// conhecer a resposta.
///
/// Toda regra deste repositorio devolve um numero e nada mais. Num problema de
/// verdade a integral certa nao e conhecida, e dizer "o erro e 2,8e-14" nao e
/// uma opcao: e preciso ESTIMAR o erro a partir da propria conta.
///
/// A maneira usual e rodar duas regras de ordens diferentes no mesmo intervalo e
/// usar a diferenca entre elas como palpite do erro da melhor. E isso levanta a
/// pergunta que este arquivo mede: esse palpite e confiavel?
///
/// Kronrod, em 1964, resolveu a parte cara do problema. A ideia e escolher a
/// regra de ordem maior de modo que ela REAPROVEITE os nos da menor, e assim a
/// estimativa de erro sai quase de graca. Aqui a versao implementada e a
/// simples, com duas regras de Gauss independentes, e a medida mostra o que ela
/// custa e o que ela acerta.
/// </summary>
public static class Kronrod
{
    /// <summary>Uma estimativa com a margem de erro que ela mesma calculou.</summary>
    public sealed class Estimativa
    {
        public Estimativa(double valor, double erroEstimado, int avaliacoes)
        {
            Valor = valor;
            ErroEstimado = erroEstimado;
            Avaliacoes = avaliacoes;
        }

        public double Valor { get; }

        /// <summary>O erro que a propria conta estima, sem conhecer a resposta.</summary>
        public double ErroEstimado { get; }

        public int Avaliacoes { get; }

        public override string ToString() => $"{Valor:N12} mais ou menos {ErroEstimado:E1}";
    }

    /// <summary>
    /// Roda duas regras de Gauss de ordens diferentes e usa a diferenca como
    /// estimativa.
    ///
    /// A regra MAIOR e a resposta, e a diferenca para a menor e o palpite de
    /// erro. Esse palpite costuma ser CONSERVADOR por uma razao simples: a
    /// diferenca entre as duas e dominada pelo erro da regra pior, que e muito
    /// maior que o da melhor.
    /// </summary>
    public static Estimativa Estimar(Func<double, double> f, double de, double ate,
                                     int pontosMenor = 7, int pontosMaior = 15)
    {
        ArgumentNullException.ThrowIfNull(f);

        var menor = Gauss.Integrar(f, de, ate, pontosMenor);
        var maior = Gauss.Integrar(f, de, ate, pontosMaior);

        return new Estimativa(maior, Math.Abs(maior - menor), pontosMenor + pontosMaior);
    }

    /// <summary>
    /// A mesma coisa em pedacos, somando os valores e os erros estimados.
    ///
    /// Somar os erros e a escolha CONSERVADORA: os erros dos pedacos podem se
    /// cancelar entre si, e somando os modulos nunca se promete menos erro do
    /// que ha. Somar com sinal daria uma estimativa menor e as vezes otimista
    /// demais, que e o pior defeito que uma estimativa de erro pode ter.
    /// </summary>
    public static Estimativa Composta(Func<double, double> f, double de, double ate, int pedacos,
                                      int pontosMenor = 7, int pontosMaior = 15)
    {
        ArgumentNullException.ThrowIfNull(f);

        double valor = 0, erro = 0;
        var avaliacoes = 0;
        var h = (ate - de) / pedacos;

        for (var i = 0; i < pedacos; i++)
        {
            var pedaco = Estimar(f, de + h * i, de + h * (i + 1), pontosMenor, pontosMaior);
            valor += pedaco.Valor;
            erro += pedaco.ErroEstimado;
            avaliacoes += pedaco.Avaliacoes;
        }

        return new Estimativa(valor, erro, avaliacoes);
    }

    /// <summary>
    /// Se a estimativa foi HONESTA, ou seja, se ela nao prometeu menos erro do
    /// que realmente houve.
    ///
    /// E a unica pergunta que importa sobre uma estimativa de erro. Uma que
    /// subestima e pior que nenhuma: ela faz o programa parar de dividir o
    /// intervalo achando que ja chegou.
    /// </summary>
    public static bool Honesta(Estimativa estimativa, double certo)
    {
        ArgumentNullException.ThrowIfNull(estimativa);
        return estimativa.ErroEstimado >= Math.Abs(estimativa.Valor - certo) - 1e-300;
    }

    /// <summary>
    /// O quanto a estimativa EXAGERA: a razao entre o erro estimado e o erro de
    /// verdade.
    ///
    /// Um exagero grande nao e defeito, e o preco da honestidade: o programa
    /// gasta mais avaliacoes do que precisaria, e nunca para antes da hora.
    /// </summary>
    public static double Exagero(Estimativa estimativa, double certo)
    {
        ArgumentNullException.ThrowIfNull(estimativa);
        var real = Math.Abs(estimativa.Valor - certo);
        return real < 1e-300 ? double.PositiveInfinity : estimativa.ErroEstimado / real;
    }
}
