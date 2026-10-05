using Conde.Integracao;
using Xunit;

namespace Conde.Integracao.Testes;

/// <summary>
/// O juiz testado antes de julgar: a integral exata de polinomios, em fracao de
/// inteiro grande. Em ponto flutuante "zero" e um numero perto de zero, e nao
/// daria para separar erro de metodo de erro de arredondamento.
/// </summary>
public class GrauTestes
{
    [Fact]
    public void AIntegralExataConfereNaMao()
    {
        // x^2 de 0 a 1 vale 1/3.
        Assert.Equal(Racional.De(1, 3), Polinomio.Monomio(2).Integral(0, 1));

        // x^3 de 0 a 2 vale 4.
        Assert.Equal(new Racional(4), Polinomio.Monomio(3).Integral(0, 2));

        // A constante 1 de 0 a 5 vale 5.
        Assert.Equal(new Racional(5), Polinomio.Monomio(0).Integral(0, 5));
    }

    [Fact]
    public void AIntegralDeUmPolinomioQualquerConfere()
    {
        // 3x^2 + 2x + 1 de 0 a 1 vale 1 + 1 + 1 = 3.
        var polinomio = new Polinomio(1, 2, 3);
        Assert.Equal(new Racional(3), polinomio.Integral(0, 1));
    }

    [Fact]
    public void AIntegralDeAAteAEhZero()
    {
        foreach (var grau in new[] { 0, 1, 5 })
            Assert.True(Polinomio.Monomio(grau).Integral(3, 3).EhZero);
    }

    [Fact]
    public void ARacionalReduzSozinha()
    {
        Assert.Equal(Racional.De(1, 2), Racional.De(2, 4));
        Assert.Equal(Racional.De(-1, 3), Racional.De(2, -6));
    }

    /// <summary>
    /// A regra de SIMPSON ajusta uma parabola e e exata ate grau TRES. E o
    /// numero que surpreende, e ele e conferido contra a integral exata.
    /// </summary>
    [Fact]
    public void ASimpsonEhExataAteGrauTres()
    {
        var simpson = Regras.Todas().First(r => r.Nome == "Simpson");
        Assert.Equal(3, Regras.GrauDeExatidao(simpson));
    }

    [Fact]
    public void OTrapezioEOPontoMedioSaoExatosAteGrauUm()
    {
        foreach (var nome in new[] { "trapezio", "ponto medio" })
        {
            var regra = Regras.Todas().First(r => r.Nome == nome);
            Assert.Equal(1, Regras.GrauDeExatidao(regra));
        }
    }

    /// <summary>
    /// O ponto medio empata com o trapezio usando METADE das avaliacoes, e isso
    /// e simetria: o que ele erra para cima de um lado do meio, erra para baixo
    /// do outro.
    /// </summary>
    [Fact]
    public void OPontoMedioEmpataComOTrapezioComMetadeDoCusto()
    {
        var pontoMedio = Regras.Todas().First(r => r.Nome == "ponto medio");
        var trapezio = Regras.Todas().First(r => r.Nome == "trapezio");

        Assert.Equal(Regras.GrauDeExatidao(trapezio), Regras.GrauDeExatidao(pontoMedio));
        Assert.True(pontoMedio.PontosPorPedaco < trapezio.PontosPorPedaco);
    }

    [Fact]
    public void ABooleEhExataAteGrauCinco()
    {
        var boole = Regras.Todas().First(r => r.Nome == "Boole");
        Assert.Equal(5, Regras.GrauDeExatidao(boole));
    }

    /// <summary>
    /// A regra de Simpson tres oitavos usa um ponto a mais que a de Simpson e
    /// tem o MESMO grau: custa mais e nao compra exatidao nenhuma.
    /// </summary>
    [Fact]
    public void ATresOitavosCustaMaisENaoCompraGrau()
    {
        var simpson = Regras.Todas().First(r => r.Nome == "Simpson");
        var tresOitavos = Regras.Todas().First(r => r.Nome == "Simpson 3/8");

        Assert.Equal(Regras.GrauDeExatidao(simpson), Regras.GrauDeExatidao(tresOitavos));
        Assert.True(tresOitavos.PontosPorPedaco > simpson.PontosPorPedaco);
    }

    /// <summary>
    /// Gauss e Legendre com n pontos e exata ate grau 2n-1, EXATAMENTE. O
    /// numero bate em toda ordem testada.
    /// </summary>
    [Fact]
    public void GaussEhExataAteDoisNMenosUm()
    {
        foreach (var pontos in new[] { 1, 2, 3, 4, 5, 6, 8, 10 })
            Assert.Equal(2 * pontos - 1, Gauss.GrauDeExatidao(pontos));
    }

    [Fact]
    public void OsPesosDeGaussSomamDois()
    {
        foreach (var pontos in new[] { 1, 2, 5, 10, 20 })
        {
            var (_, pesos) = Gauss.NosEPesos(pontos);
            Assert.Equal(2, pesos.Sum(), 1e-12);
        }
    }

    /// <summary>
    /// Os nos de Gauss sao SIMETRICOS em volta de zero, e os pesos tambem. E
    /// uma conferencia que nao depende de nenhuma integral.
    /// </summary>
    [Fact]
    public void OsNosDeGaussSaoSimetricos()
    {
        foreach (var pontos in new[] { 2, 3, 5, 8 })
        {
            var (nos, pesos) = Gauss.NosEPesos(pontos);
            for (var i = 0; i < pontos; i++)
            {
                Assert.Equal(-nos[i], nos[pontos - 1 - i], 1e-12);
                Assert.Equal(pesos[i], pesos[pontos - 1 - i], 1e-12);
            }
        }
    }

    [Fact]
    public void OsNosDeGaussFicamDentroDoIntervalo()
    {
        foreach (var pontos in new[] { 1, 3, 7, 15 })
            Assert.All(Gauss.NosEPesos(pontos).Nos, no => Assert.InRange(no, -1, 1));
    }

    [Fact]
    public void OPolinomioDeLegendreValeUmEmUm()
    {
        foreach (var grau in new[] { 1, 2, 5, 10 })
            Assert.Equal(1, Gauss.Legendre(grau, 1.0 - 1e-12).Valor, 1e-6);
    }
}
