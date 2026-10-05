namespace Conde.Integracao;

/// <summary>
/// ROMBERG: a regra do trapezio mais a extrapolacao de Richardson.
///
/// A ideia e boa demais para ser tao pouco conhecida. O erro do trapezio tem uma
/// forma previsivel: ele e proporcional a h ao quadrado, mais um termo em h a
/// quarta, mais um em h a sexta. Isso quer dizer que o trapezio com passo h e o
/// com passo h/2 erram de maneiras RELACIONADAS, e combinando os dois da para
/// matar o termo de h ao quadrado inteiro.
///
/// O resultado dessa combinacao e exatamente a regra de Simpson. Combinando de
/// novo, mata-se o termo de h a quarta, e sai a regra de Boole. A tabela de
/// Romberg e essa combinacao repetida, e cada coluna nova mata mais um termo.
///
/// Com cinco niveis, a partir de trapezios que erram na terceira casa, sai um
/// resultado com treze casas certas. A medida mostra a tabela inteira.
/// </summary>
public static class Romberg
{
    /// <summary>A tabela de Romberg: cada linha dobra os pedacos, cada coluna extrapola.</summary>
    public static double[,] Tabela(Func<double, double> f, double de, double ate, int niveis)
    {
        ArgumentNullException.ThrowIfNull(f);
        if (niveis < 1) throw new ArgumentOutOfRangeException(nameof(niveis));

        var tabela = new double[niveis, niveis];

        for (var i = 0; i < niveis; i++)
        {
            tabela[i, 0] = Regras.Trapezio(f, de, ate, 1 << i);

            for (var j = 1; j <= i; j++)
            {
                // A extrapolacao de Richardson: a combinacao que mata o termo de
                // ordem 2j do erro. O 4^j nao e ajuste: ele e a razao entre os
                // erros de dois passos que diferem por um fator de dois.
                var fator = Math.Pow(4, j);
                tabela[i, j] = (fator * tabela[i, j - 1] - tabela[i - 1, j - 1]) / (fator - 1);
            }
        }

        return tabela;
    }

    public static double Integrar(Func<double, double> f, double de, double ate, int niveis = 8)
    {
        var tabela = Tabela(f, de, ate, niveis);
        return tabela[niveis - 1, niveis - 1];
    }

    /// <summary>Quantas avaliacoes de funcao a tabela gasta.</summary>
    public static int Avaliacoes(int niveis) => (1 << (niveis - 1)) + niveis;

    /// <summary>
    /// A primeira extrapolacao do trapezio E a regra de Simpson, exatamente.
    ///
    /// Isso nao e semelhanca: as duas formulas sao a mesma formula escrita de
    /// dois jeitos, e um teste confere a igualdade em muitos casos.
    /// </summary>
    public static double PrimeiraExtrapolacao(Func<double, double> f, double de, double ate, int pedacos)
    {
        ArgumentNullException.ThrowIfNull(f);
        var grosso = Regras.Trapezio(f, de, ate, pedacos);
        var fino = Regras.Trapezio(f, de, ate, pedacos * 2);
        return (4 * fino - grosso) / 3;
    }
}

/// <summary>
/// A quadratura ADAPTATIVA: ela decide sozinha onde gastar avaliacoes.
///
/// A ideia e simples: calcula a integral de um pedaco com Simpson, calcula de
/// novo com os dois meios-pedacos, e compara. Se os dois resultados estao perto,
/// o pedaco esta resolvido; se nao, divide e repete em cada metade.
///
/// Numa funcao lisa ela gasta quase nada e numa funcao com um pico estreito ela
/// gasta tudo em volta do pico, que e exatamente onde precisa. Uma regra fixa
/// com o mesmo numero de avaliacoes espalha os pontos por igual e pode nao
/// encostar no pico.
/// </summary>
public static class Adaptativa
{
    /// <summary>O resultado, com a conta de quanto trabalho deu.</summary>
    public sealed class Saida
    {
        public Saida(double valor, int avaliacoes, int pedacos, double menorPedaco)
        {
            Valor = valor;
            Avaliacoes = avaliacoes;
            Pedacos = pedacos;
            MenorPedaco = menorPedaco;
        }

        public double Valor { get; }
        public int Avaliacoes { get; }
        public int Pedacos { get; }

        /// <summary>O menor pedaco que ela precisou criar. E onde esta a dificuldade.</summary>
        public double MenorPedaco { get; }

        public override string ToString() =>
            $"{Valor:N10} com {Avaliacoes} avaliacoes em {Pedacos} pedacos";
    }

    public static Saida Integrar(Func<double, double> f, double de, double ate,
                                 double tolerancia = 1e-10, int fundoMaximo = 50)
    {
        ArgumentNullException.ThrowIfNull(f);

        var avaliacoes = 0;
        var pedacos = 0;
        var menor = ate - de;

        double Avaliar(double x)
        {
            avaliacoes++;
            return f(x);
        }

        double Dividir(double a, double b, double tol, int fundo)
        {
            var meio = (a + b) / 2;
            var inteiro = Simpson(Avaliar, a, b);
            var esquerda = Simpson(Avaliar, a, meio);
            var direita = Simpson(Avaliar, meio, b);

            // O criterio de parada divide por quinze, e esse numero vem da
            // teoria e nao de ajuste: a diferenca entre as duas estimativas e
            // quinze vezes o erro da mais fina, porque Simpson erra como h a
            // quarta e dividir ao meio reduz isso por dezesseis.
            if (fundo >= fundoMaximo || Math.Abs(esquerda + direita - inteiro) <= 15 * tol)
            {
                pedacos++;
                menor = Math.Min(menor, b - a);
                return esquerda + direita + (esquerda + direita - inteiro) / 15;
            }

            return Dividir(a, meio, tol / 2, fundo + 1) + Dividir(meio, b, tol / 2, fundo + 1);
        }

        var valor = Dividir(de, ate, tolerancia, 0);
        return new Saida(valor, avaliacoes, pedacos, menor);
    }

    private static double Simpson(Func<double, double> f, double a, double b) =>
        (b - a) / 6 * (f(a) + 4 * f((a + b) / 2) + f(b));
}

/// <summary>As funcoes de teste.</summary>
public static class Casos
{
    /// <summary>Uma funcao lisa e bem comportada, com integral conhecida.</summary>
    public static (string Nome, Func<double, double> F, double De, double Ate, double Certo) Exponencial() =>
        ("exp(x) de 0 a 1", Math.Exp, 0, 1, Math.E - 1);

    /// <summary>O seno, cuja integral de zero a pi vale exatamente dois.</summary>
    public static (string Nome, Func<double, double> F, double De, double Ate, double Certo) Seno() =>
        ("sen(x) de 0 a pi", Math.Sin, 0, Math.PI, 2);

    /// <summary>
    /// Um PICO ESTREITO: 1/(1 + 10000*(x-0.5)^2), que sobe e desce numa faixa
    /// de um centesimo do intervalo.
    ///
    /// E o caso que separa a regra fixa da adaptativa: com poucos pontos
    /// igualmente espacados, nenhum deles encosta no pico e a integral sai perto
    /// de zero.
    /// </summary>
    public static (string Nome, Func<double, double> F, double De, double Ate, double Certo) Pico()
    {
        static double f(double x) => 1.0 / (1 + 10000 * (x - 0.5) * (x - 0.5));
        var certo = (Math.Atan(50.0) - Math.Atan(-50.0)) / 100;
        return ("pico estreito em 0,5", f, 0, 1, certo);
    }

    /// <summary>
    /// A raiz quadrada, cuja DERIVADA e infinita em zero.
    ///
    /// A funcao e integravel sem problema nenhum, e todas as regras sofrem,
    /// porque elas supoem que a funcao se parece com um polinomio e nenhuma
    /// potencia inteira se parece com raiz perto de zero.
    /// </summary>
    public static (string Nome, Func<double, double> F, double De, double Ate, double Certo) Raiz() =>
        ("raiz de x, de 0 a 1", Math.Sqrt, 0, 1, 2.0 / 3);

    public static IEnumerable<(string Nome, Func<double, double> F, double De, double Ate, double Certo)> Todos()
    {
        yield return Exponencial();
        yield return Seno();
        yield return Pico();
        yield return Raiz();
    }
}
