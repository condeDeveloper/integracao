namespace Conde.Integracao;

/// <summary>
/// A quadratura de GAUSS E LEGENDRE: em vez de aceitar pontos igualmente
/// espacados, ela ESCOLHE onde avaliar.
///
/// Uma regra de n pontos tem 2n numeros livres: n posicoes e n pesos. As regras
/// de Newton e Cotes fixam as posicoes e sobram n liberdades, o que da exatidao
/// ate grau n-1 mais ou menos. Gauss usa as 2n liberdades e chega a grau
/// 2n MENOS UM.
///
/// Com tres pontos, uma regra de Newton e Cotes acerta ate grau tres e esta
/// acerta ate grau CINCO. Com cinco pontos, nove. A medida confirma os dois
/// numeros contra a integral exata.
///
/// Os nos sao as raizes do polinomio de Legendre de grau n, e nao ha formula
/// fechada: eles sao achados por Newton. E vale notar que eles sao irracionais,
/// entao a regra nao tem como ser exata em aritmetica exata, so em ponto
/// flutuante.
/// </summary>
public static class Gauss
{
    /// <summary>Os nos e os pesos de uma regra de n pontos, no intervalo de -1 a 1.</summary>
    public static (double[] Nos, double[] Pesos) NosEPesos(int pontos)
    {
        if (pontos < 1) throw new ArgumentOutOfRangeException(nameof(pontos));

        var nos = new double[pontos];
        var pesos = new double[pontos];

        for (var i = 0; i < pontos; i++)
        {
            // O chute inicial e a aproximacao classica das raizes, boa o
            // bastante para Newton fechar em meia duzia de voltas.
            var x = Math.Cos(Math.PI * (i + 0.75) / (pontos + 0.5));

            for (var volta = 0; volta < 100; volta++)
            {
                var (valor, derivada) = Legendre(pontos, x);
                var passo = valor / derivada;
                x -= passo;
                if (Math.Abs(passo) < 1e-15) break;
            }

            var (_, derivadaFinal) = Legendre(pontos, x);
            nos[i] = x;
            pesos[i] = 2 / ((1 - x * x) * derivadaFinal * derivadaFinal);
        }

        Array.Sort(nos, pesos);
        return (nos, pesos);
    }

    /// <summary>
    /// O polinomio de Legendre de grau n e a derivada dele, pela recorrencia de
    /// Bonnet.
    ///
    /// A recorrencia e usada em vez da formula fechada por estabilidade: a
    /// formula com fatoriais perde digitos rapido, e a recorrencia e so somas e
    /// multiplicacoes de numeros da mesma ordem.
    /// </summary>
    public static (double Valor, double Derivada) Legendre(int grau, double x)
    {
        double anterior = 1;
        double atual = x;

        if (grau == 0) return (1, 0);

        for (var k = 2; k <= grau; k++)
        {
            var proximo = ((2 * k - 1) * x * atual - (k - 1) * anterior) / k;
            anterior = atual;
            atual = proximo;
        }

        var derivada = grau * (x * atual - anterior) / (x * x - 1);
        return (atual, derivada);
    }

    /// <summary>A integral de f de a ate b, com n pontos.</summary>
    public static double Integrar(Func<double, double> f, double de, double ate, int pontos)
    {
        ArgumentNullException.ThrowIfNull(f);

        var (nos, pesos) = NosEPesos(pontos);
        var meio = (de + ate) / 2;
        var metade = (ate - de) / 2;

        double soma = 0;
        for (var i = 0; i < pontos; i++) soma += pesos[i] * f(meio + metade * nos[i]);
        return soma * metade;
    }

    /// <summary>
    /// A versao COMPOSTA: divide o intervalo em pedacos e aplica a regra de n
    /// pontos em cada um.
    ///
    /// Ela existe porque a regra de muitos pontos num intervalo grande sofre do
    /// mesmo problema da interpolacao de grau alto. Pedacos pequenos com poucos
    /// pontos cada sao mais confiaveis que um pedaco so com muitos.
    /// </summary>
    public static double Composta(Func<double, double> f, double de, double ate, int pontos, int pedacos)
    {
        ArgumentNullException.ThrowIfNull(f);

        var h = (ate - de) / pedacos;
        double soma = 0;
        for (var i = 0; i < pedacos; i++) soma += Integrar(f, de + h * i, de + h * (i + 1), pontos);
        return soma;
    }

    /// <summary>
    /// O grau de exatidao medido, conferido contra a integral exata do
    /// polinomio.
    /// </summary>
    public static int GrauDeExatidao(int pontos, int ateGrau = 30)
    {
        for (var grau = 0; grau <= ateGrau; grau++)
        {
            var polinomio = Polinomio.Monomio(grau);
            var certo = polinomio.Integral(Racional.Zero, Racional.Um).Valor();
            var achado = Integrar(polinomio.Avaliar, 0, 1, pontos);

            if (Math.Abs(achado - certo) > 1e-12 * Math.Max(1, Math.Abs(certo))) return grau - 1;
        }
        return ateGrau;
    }

    /// <summary>Quantas avaliacoes de funcao a regra composta gasta.</summary>
    public static int Avaliacoes(int pontos, int pedacos) => pontos * pedacos;
}
