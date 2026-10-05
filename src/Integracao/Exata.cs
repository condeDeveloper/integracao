using System.Numerics;

namespace Conde.Integracao;

/// <summary>Uma fracao de inteiro grande, sempre reduzida.</summary>
public readonly struct Racional : IEquatable<Racional>
{
    private readonly BigInteger baixo;

    public Racional(BigInteger cima, BigInteger baixo)
    {
        if (baixo.IsZero) throw new DivideByZeroException("racional com denominador zero");

        if (baixo.Sign < 0)
        {
            cima = -cima;
            baixo = -baixo;
        }

        var divisor = BigInteger.GreatestCommonDivisor(BigInteger.Abs(cima), baixo);
        if (!divisor.IsZero && !divisor.IsOne)
        {
            cima /= divisor;
            baixo /= divisor;
        }

        Cima = cima;
        this.baixo = baixo;
    }

    public Racional(BigInteger inteiro) : this(inteiro, BigInteger.One) { }

    public BigInteger Cima { get; }

    public BigInteger Baixo => baixo.IsZero ? BigInteger.One : baixo;

    public static Racional Zero => new(BigInteger.Zero);
    public static Racional Um => new(BigInteger.One);

    public bool EhZero => Cima.IsZero;

    public static Racional De(long cima, long baixo) => new(cima, baixo);

    public static implicit operator Racional(int inteiro) => new(inteiro);

    public static Racional operator +(Racional a, Racional b) =>
        new(a.Cima * b.Baixo + b.Cima * a.Baixo, a.Baixo * b.Baixo);

    public static Racional operator -(Racional a, Racional b) =>
        new(a.Cima * b.Baixo - b.Cima * a.Baixo, a.Baixo * b.Baixo);

    public static Racional operator *(Racional a, Racional b) => new(a.Cima * b.Cima, a.Baixo * b.Baixo);

    public static Racional operator /(Racional a, Racional b) => new(a.Cima * b.Baixo, a.Baixo * b.Cima);

    public double Valor()
    {
        if (BigInteger.Abs(Cima) < BigInteger.Pow(2, 900) && Baixo < BigInteger.Pow(2, 900))
            return (double)Cima / (double)Baixo;

        var sinal = Cima.Sign;
        return sinal * Math.Exp(BigInteger.Log(BigInteger.Abs(Cima)) - BigInteger.Log(Baixo));
    }

    public bool Equals(Racional outro) => Cima == outro.Cima && Baixo == outro.Baixo;

    public override bool Equals(object? obj) => obj is Racional outro && Equals(outro);

    public override int GetHashCode() => HashCode.Combine(Cima, Baixo);

    public static bool operator ==(Racional a, Racional b) => a.Equals(b);

    public static bool operator !=(Racional a, Racional b) => !a.Equals(b);

    public override string ToString() => Baixo.IsOne ? Cima.ToString() : $"{Cima}/{Baixo}";
}

/// <summary>
/// Um polinomio em coeficientes racionais, com a integral EXATA.
///
/// Ele e o juiz deste repositorio. A pergunta central e "esta regra e exata ate
/// que grau?", e ela so tem resposta com uma integral que nao erra nada: em
/// ponto flutuante, "zero" e um numero perto de zero e ninguem sabe se o metodo
/// errou ou se o arredondamento errou.
///
/// Com a integral exata a resposta e binaria: ou a diferenca e exatamente zero
/// ou nao e.
/// </summary>
public sealed class Polinomio
{
    public Polinomio(params Racional[] coeficientes)
    {
        ArgumentNullException.ThrowIfNull(coeficientes);
        var lista = coeficientes.ToList();
        while (lista.Count > 1 && lista[^1].EhZero) lista.RemoveAt(lista.Count - 1);
        Coeficientes = lista;
    }

    public IReadOnlyList<Racional> Coeficientes { get; }

    public int Grau => Coeficientes.Count - 1;

    /// <summary>O monomio x elevado ao grau dado.</summary>
    public static Polinomio Monomio(int grau)
    {
        var coeficientes = new Racional[grau + 1];
        coeficientes[grau] = Racional.Um;
        return new Polinomio(coeficientes);
    }

    public Racional Avaliar(Racional x)
    {
        var saida = Racional.Zero;
        for (var i = Coeficientes.Count - 1; i >= 0; i--) saida = saida * x + Coeficientes[i];
        return saida;
    }

    public double Avaliar(double x)
    {
        double saida = 0;
        for (var i = Coeficientes.Count - 1; i >= 0; i--) saida = saida * x + Coeficientes[i].Valor();
        return saida;
    }

    /// <summary>
    /// A integral EXATA de a ate b, pela regra do colegio: cada termo vira o
    /// seguinte dividido pelo expoente novo.
    /// </summary>
    public Racional Integral(Racional de, Racional ate)
    {
        var soma = Racional.Zero;
        for (var i = 0; i < Coeficientes.Count; i++)
        {
            if (Coeficientes[i].EhZero) continue;
            var expoente = i + 1;
            var fator = Coeficientes[i] / new Racional(expoente);
            soma += fator * (Potencia(ate, expoente) - Potencia(de, expoente));
        }
        return soma;
    }

    private static Racional Potencia(Racional baixo, int expoente)
    {
        var saida = Racional.Um;
        for (var i = 0; i < expoente; i++) saida *= baixo;
        return saida;
    }

    public override string ToString()
    {
        var partes = new List<string>();
        for (var i = Coeficientes.Count - 1; i >= 0; i--)
        {
            if (Coeficientes[i].EhZero) continue;
            partes.Add(i == 0 ? Coeficientes[i].ToString()
                      : i == 1 ? $"{Coeficientes[i]}x" : $"{Coeficientes[i]}x^{i}");
        }
        return partes.Count == 0 ? "0" : string.Join(" + ", partes);
    }
}
