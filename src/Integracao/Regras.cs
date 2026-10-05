namespace Conde.Integracao;

/// <summary>
/// As regras de NEWTON E COTES: aproximar a funcao por um polinomio que passa
/// por pontos IGUALMENTE ESPACADOS e integrar esse polinomio.
///
/// Todas elas tem a mesma receita e mudam so quantos pontos entram. E todas tem
/// um GRAU DE EXATIDAO: o maior grau de polinomio que elas integram sem erro
/// nenhum.
///
/// O numero que surpreende e o da regra de Simpson. Ela ajusta uma PARABOLA aos
/// pontos, entao a resposta obvia seria grau dois. Ela e exata ate grau TRES: o
/// erro do termo cubico se cancela por simetria, de graca. A medida confirma
/// isso em aritmetica exata, sem margem para arredondamento.
/// </summary>
public static class Regras
{
    /// <summary>Uma regra com nome, para as varreduras.</summary>
    public sealed class Regra
    {
        public Regra(string nome, int pontosPorPedaco, Func<Func<double, double>, double, double, int, double> rodar)
        {
            Nome = nome;
            PontosPorPedaco = pontosPorPedaco;
            Rodar = rodar;
        }

        public string Nome { get; }

        /// <summary>Quantos pontos a regra usa em cada pedaco.</summary>
        public int PontosPorPedaco { get; }

        public Func<Func<double, double>, double, double, int, double> Rodar { get; }

        public override string ToString() => Nome;
    }

    /// <summary>
    /// PONTO MEDIO: a area do retangulo com a altura do meio do intervalo.
    ///
    /// Ela parece a mais grosseira das regras e e exata ate grau UM, igual ao
    /// trapezio, com METADE das avaliacoes. O motivo e simetria: o que ela erra
    /// para cima de um lado do meio, ela erra para baixo do outro.
    /// </summary>
    public static double PontoMedio(Func<double, double> f, double de, double ate, int pedacos)
    {
        ArgumentNullException.ThrowIfNull(f);
        var h = (ate - de) / pedacos;
        double soma = 0;
        for (var i = 0; i < pedacos; i++) soma += f(de + h * (i + 0.5));
        return soma * h;
    }

    /// <summary>TRAPEZIO: a area do trapezio entre os dois extremos.</summary>
    public static double Trapezio(Func<double, double> f, double de, double ate, int pedacos)
    {
        ArgumentNullException.ThrowIfNull(f);
        var h = (ate - de) / pedacos;
        var soma = (f(de) + f(ate)) / 2;
        for (var i = 1; i < pedacos; i++) soma += f(de + h * i);
        return soma * h;
    }

    /// <summary>
    /// SIMPSON: a parabola pelos dois extremos e pelo meio.
    ///
    /// Os pesos sao 1, 4, 1 divididos por seis, e o quatro do meio e o que faz
    /// ela ser exata ate grau tres em vez de dois.
    /// </summary>
    public static double Simpson(Func<double, double> f, double de, double ate, int pedacos)
    {
        ArgumentNullException.ThrowIfNull(f);
        if (pedacos % 2 != 0) pedacos++;

        var h = (ate - de) / pedacos;
        var soma = f(de) + f(ate);
        for (var i = 1; i < pedacos; i++) soma += f(de + h * i) * (i % 2 == 0 ? 2 : 4);
        return soma * h / 3;
    }

    /// <summary>
    /// SIMPSON TRES OITAVOS: a cubica por quatro pontos.
    ///
    /// Ela usa mais um ponto por pedaco que a de Simpson e tem o MESMO grau de
    /// exatidao, tres. E uma regra que custa mais e nao compra nada em exatidao,
    /// e existe por outro motivo: ela fecha pedacos em multiplos de tres, o que
    /// ajuda quando o numero de pontos nao e par.
    /// </summary>
    public static double TresOitavos(Func<double, double> f, double de, double ate, int pedacos)
    {
        ArgumentNullException.ThrowIfNull(f);
        while (pedacos % 3 != 0) pedacos++;

        var h = (ate - de) / pedacos;
        var soma = f(de) + f(ate);
        for (var i = 1; i < pedacos; i++) soma += f(de + h * i) * (i % 3 == 0 ? 2 : 3);
        return soma * h * 3 / 8;
    }

    /// <summary>
    /// BOOLE: a quartica por cinco pontos, com pesos 7, 32, 12, 32, 7.
    ///
    /// Grau de exatidao CINCO, e nao quatro, pelo mesmo cancelamento de simetria
    /// que faz Simpson chegar a tres. Toda regra de Newton e Cotes com numero
    /// IMPAR de pontos ganha um grau de brinde.
    /// </summary>
    public static double Boole(Func<double, double> f, double de, double ate, int pedacos)
    {
        ArgumentNullException.ThrowIfNull(f);
        while (pedacos % 4 != 0) pedacos++;

        var h = (ate - de) / pedacos;
        double soma = 0;
        for (var i = 0; i < pedacos; i += 4)
        {
            var x = de + h * i;
            soma += 7 * f(x) + 32 * f(x + h) + 12 * f(x + 2 * h) + 32 * f(x + 3 * h) + 7 * f(x + 4 * h);
        }
        return soma * 2 * h / 45;
    }

    public static IEnumerable<Regra> Todas()
    {
        yield return new Regra("ponto medio", 1, PontoMedio);
        yield return new Regra("trapezio", 2, Trapezio);
        yield return new Regra("Simpson", 3, Simpson);
        yield return new Regra("Simpson 3/8", 4, TresOitavos);
        yield return new Regra("Boole", 5, Boole);
    }

    /// <summary>
    /// O GRAU DE EXATIDAO medido: o maior grau de monomio que a regra integra
    /// sem erro nenhum, conferido contra a integral exata.
    ///
    /// A conferencia usa uma folga minuscula e nao zero absoluto, e vale dizer
    /// por que: a regra roda em ponto flutuante mesmo quando o resultado certo e
    /// racional, entao mesmo exata ela devolve um numero com erro de
    /// arredondamento. O que a medida separa e "erro de arredondamento" de "erro
    /// de metodo", que diferem em ordens de grandeza.
    /// </summary>
    public static int GrauDeExatidao(Regra regra, double de = 0, double ate = 1, int ate_grau = 12)
    {
        ArgumentNullException.ThrowIfNull(regra);

        for (var grau = 0; grau <= ate_grau; grau++)
        {
            var polinomio = Polinomio.Monomio(grau);
            var certo = polinomio.Integral(new Racional((int)de), new Racional((int)ate)).Valor();
            var achado = regra.Rodar(polinomio.Avaliar, de, ate, regra.PontosPorPedaco * 2);

            if (Math.Abs(achado - certo) > 1e-12 * Math.Max(1, Math.Abs(certo))) return grau - 1;
        }

        return ate_grau;
    }
}
