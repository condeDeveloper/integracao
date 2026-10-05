using Conde.Integracao;
using Xunit;

namespace Conde.Integracao.Testes;

public class RombergTestes
{
    private const double ExpCerto = Math.E - 1;

    /// <summary>
    /// A primeira extrapolacao do trapezio E a regra de Simpson, exatamente. Nao
    /// e semelhanca: as duas formulas sao a mesma formula escrita de dois jeitos.
    /// </summary>
    [Fact]
    public void APrimeiraExtrapolacaoEhARegraDeSimpson()
    {
        foreach (var (nome, f, de, ate, _) in Casos.Todos())
            foreach (var pedacos in new[] { 2, 4, 8, 16 })
            {
                var extrapolado = Romberg.PrimeiraExtrapolacao(f, de, ate, pedacos);
                var simpson = Regras.Simpson(f, de, ate, pedacos * 2);
                Assert.Equal(simpson, extrapolado, Math.Abs(simpson) * 1e-12 + 1e-14);
            }
    }

    /// <summary>
    /// Cada coluna da tabela melhora sobre a anterior, e a diagonal converge
    /// para o limite do double.
    /// </summary>
    [Fact]
    public void CadaColunaMelhoraSobreAAnterior()
    {
        var tabela = Romberg.Tabela(Math.Exp, 0, 1, 6);

        for (var i = 1; i < 6; i++)
            for (var j = 1; j <= i; j++)
            {
                var antes = Math.Abs(tabela[i, j - 1] - ExpCerto);
                var depois = Math.Abs(tabela[i, j] - ExpCerto);
                Assert.True(depois <= antes + 1e-16, $"linha {i}, coluna {j}");
            }
    }

    [Fact]
    public void ADiagonalChegaAoLimiteDoDouble()
    {
        var tabela = Romberg.Tabela(Math.Exp, 0, 1, 6);
        Assert.True(Math.Abs(tabela[5, 5] - ExpCerto) < 1e-15);
    }

    /// <summary>
    /// A coluna zero e o trapezio puro, e dobrar os pedacos divide o erro por
    /// QUATRO. E dessa relacao que a extrapolacao tira os digitos.
    /// </summary>
    [Fact]
    public void OTrapezioDivideOErroPorQuatroAoDobrarOsPedacos()
    {
        var tabela = Romberg.Tabela(Math.Exp, 0, 1, 7);

        for (var i = 2; i < 7; i++)
        {
            var antes = Math.Abs(tabela[i - 1, 0] - ExpCerto);
            var depois = Math.Abs(tabela[i, 0] - ExpCerto);
            Assert.InRange(antes / depois, 3.8, 4.2);
        }
    }

    [Fact]
    public void RombergAcertaAsFuncoesLisas()
    {
        foreach (var (nome, f, de, ate, certo) in new[] { Casos.Exponencial(), Casos.Seno() })
        {
            var achado = Romberg.Integrar(f, de, ate, 10);
            Assert.True(Math.Abs(achado - certo) < 1e-12, $"{nome}: deu {achado:N15}");
        }
    }

    [Fact]
    public void AsRegrasFixasAcertamAsFuncoesLisas()
    {
        foreach (var (nome, f, de, ate, certo) in new[] { Casos.Exponencial(), Casos.Seno() })
            foreach (var regra in Regras.Todas())
            {
                var achado = regra.Rodar(f, de, ate, 1000);
                Assert.True(Math.Abs(achado - certo) < 1e-5, $"{regra.Nome} em {nome}");
            }
    }

    /// <summary>
    /// Com as MESMAS mil avaliacoes, a diferenca entre as regras e de seis
    /// ordens de grandeza.
    ///
    /// Eu escrevi que todas acertam funcao lisa e exigi erro abaixo de um
    /// milionesimo, e o trapezio no seno da 1,6 milionesimos: ele passa raspando
    /// de uma exigencia que Simpson supera por um fator de um milhao. "Acerta" e
    /// uma palavra que esconde essa distancia.
    /// </summary>
    [Fact]
    public void ComAsMesmasAvaliacoesADiferencaEhDeSeisOrdens()
    {
        var (_, f, de, ate, certo) = Casos.Seno();

        var trapezio = Math.Abs(Regras.Trapezio(f, de, ate, 1000) - certo);
        var simpson = Math.Abs(Regras.Simpson(f, de, ate, 1000) - certo);

        Assert.True(trapezio > simpson * 100_000, $"trapezio {trapezio:E2}, Simpson {simpson:E2}");
        Assert.True(trapezio > 1e-6, "e o trapezio nem chega a um milionesimo");
    }

    [Fact]
    public void AAdaptativaAcertaTodosOsCasos()
    {
        foreach (var (nome, f, de, ate, certo) in Casos.Todos())
        {
            var saida = Adaptativa.Integrar(f, de, ate);
            Assert.True(Math.Abs(saida.Valor - certo) < 1e-10, $"{nome}: erro {Math.Abs(saida.Valor - certo):E2}");
        }
    }

    /// <summary>
    /// A adaptativa aperta os pedacos ONDE a funcao e dificil. Na raiz de x ela
    /// desce a pedacos minusculos perto do zero, e na exponencial para cedo.
    /// </summary>
    [Fact]
    public void AAdaptativaApertaOndeAFuncaoEhDificil()
    {
        var lisa = Adaptativa.Integrar(Math.Exp, 0, 1);
        var dificil = Adaptativa.Integrar(Math.Sqrt, 0, 1);

        Assert.True(dificil.MenorPedaco < lisa.MenorPedaco / 1000);
        Assert.True(dificil.Avaliacoes > lisa.Avaliacoes);
    }

    /// <summary>
    /// O caso em que a adaptativa ganha de verdade: a raiz de x, cuja derivada
    /// e infinita em zero. Com o MESMO numero de avaliacoes, ela erra ordens de
    /// grandeza menos.
    /// </summary>
    [Fact]
    public void NaRaizDeXAAdaptativaGanhaDeLonge()
    {
        var adaptativa = Adaptativa.Integrar(Math.Sqrt, 0, 1);
        var fixa = Regras.Simpson(Math.Sqrt, 0, 1, adaptativa.Avaliacoes);

        var erroAdaptativo = Math.Abs(adaptativa.Valor - 2.0 / 3);
        var erroFixo = Math.Abs(fixa - 2.0 / 3);

        Assert.True(erroFixo > erroAdaptativo * 1_000_000, $"fixa {erroFixo:E1}, adaptativa {erroAdaptativo:E1}");
    }

    /// <summary>
    /// No pico estreito, com poucos pontos, a regra fixa devolve um numero que
    /// nao tem relacao com a resposta: nenhum dos pontos cai perto do pico.
    /// </summary>
    [Fact]
    public void NoPicoEstreitoARegraFixaComPoucosPontosErraFeio()
    {
        var (_, f, de, ate, certo) = Casos.Pico();
        var comDez = Regras.Simpson(f, de, ate, 10);

        Assert.True(comDez > certo * 3, $"deu {comDez:N6} para um certo de {certo:N6}");
    }

    /// <summary>
    /// E com orcamento grande ela alcanca. Isso me corrigiu: eu ia escrever que
    /// a adaptativa ganha sempre no pico.
    /// </summary>
    [Fact]
    public void ComOrcamentoGrandeARegraFixaAlcancaNoPico()
    {
        var (_, f, de, ate, certo) = Casos.Pico();
        Assert.True(Math.Abs(Regras.Simpson(f, de, ate, 320) - certo) < 1e-5);
    }

    [Fact]
    public void MaisToleranciaCustaMenosAvaliacoes()
    {
        var (_, f, de, ate, _) = Casos.Pico();
        var folgado = Adaptativa.Integrar(f, de, ate, 1e-4);
        var apertado = Adaptativa.Integrar(f, de, ate, 1e-12);

        Assert.True(apertado.Avaliacoes > folgado.Avaliacoes * 10);
    }

    [Fact]
    public void AGaussCompostaConcordaComAGaussDeUmPedacoSo()
    {
        foreach (var pontos in new[] { 3, 5 })
        {
            var unica = Gauss.Integrar(Math.Exp, 0, 1, pontos);
            var composta = Gauss.Composta(Math.Exp, 0, 1, pontos, 1);
            Assert.Equal(unica, composta, 1e-14);
        }
    }

    [Fact]
    public void AGaussCompostaMelhoraComMaisPedacos()
    {
        double anterior = double.MaxValue;
        foreach (var pedacos in new[] { 1, 2, 4 })
        {
            var erro = Math.Abs(Gauss.Composta(Math.Sqrt, 0, 1, 3, pedacos) - 2.0 / 3);
            Assert.True(erro < anterior);
            anterior = erro;
        }
    }
}
