using Conde.Integracao;
using Xunit;

namespace Conde.Integracao.Testes;

/// <summary>
/// A unica pergunta que importa sobre uma estimativa de erro: ela e HONESTA?
///
/// Uma estimativa que subestima e pior que nenhuma, porque ela faz o programa
/// parar de dividir o intervalo achando que ja chegou. Exagerar e so caro.
/// </summary>
public class KronrodTestes
{
    [Fact]
    public void AEstimativaEhHonestaEmTodosOsCasos()
    {
        foreach (var (nome, f, de, ate, certo) in Casos.Todos())
        {
            var estimativa = Kronrod.Estimar(f, de, ate);
            Assert.True(Kronrod.Honesta(estimativa, certo),
                        $"{nome}: estimou {estimativa.ErroEstimado:E1} e errou {Math.Abs(estimativa.Valor - certo):E1}");
        }
    }

    [Fact]
    public void AEstimativaCompostaTambemEhHonesta()
    {
        foreach (var (nome, f, de, ate, certo) in Casos.Todos())
            foreach (var pedacos in new[] { 1, 4, 16, 64 })
            {
                var estimativa = Kronrod.Composta(f, de, ate, pedacos);
                Assert.True(Kronrod.Honesta(estimativa, certo), $"{nome} com {pedacos} pedacos");
            }
    }

    /// <summary>
    /// E ela EXAGERA, que e o preco da honestidade: o programa gasta mais
    /// avaliacoes do que precisaria e nunca para antes da hora.
    /// </summary>
    [Fact]
    public void AEstimativaExageraEmVezDeSubestimar()
    {
        var (_, f, de, ate, certo) = Casos.Seno();
        var estimativa = Kronrod.Estimar(f, de, ate);
        Assert.True(Kronrod.Exagero(estimativa, certo) > 10);
    }

    [Fact]
    public void OValorDevolvidoEhODaRegraMaior()
    {
        foreach (var (_, f, de, ate, _) in Casos.Todos())
        {
            var estimativa = Kronrod.Estimar(f, de, ate, 7, 15);
            Assert.Equal(Gauss.Integrar(f, de, ate, 15), estimativa.Valor, 1e-15);
        }
    }

    [Fact]
    public void AsAvaliacoesSaoASomaDasDuasRegras()
    {
        var estimativa = Kronrod.Estimar(Math.Exp, 0, 1, 7, 15);
        Assert.Equal(22, estimativa.Avaliacoes);

        var composta = Kronrod.Composta(Math.Exp, 0, 1, 4, 7, 15);
        Assert.Equal(88, composta.Avaliacoes);
    }

    /// <summary>
    /// Mais pedacos derruba o erro ESTIMADO junto com o real, que e o que
    /// permite usar a estimativa como criterio de parada.
    /// </summary>
    [Fact]
    public void MaisPedacosDerrubamOErroEstimado()
    {
        var (_, f, de, ate, _) = Casos.Pico();
        double anterior = double.MaxValue;

        foreach (var pedacos in new[] { 1, 4, 16, 64 })
        {
            var estimativa = Kronrod.Composta(f, de, ate, pedacos);
            Assert.True(estimativa.ErroEstimado < anterior, $"com {pedacos} pedacos");
            anterior = estimativa.ErroEstimado;
        }
    }

    /// <summary>
    /// Numa funcao que as duas regras integram exatamente, a estimativa da
    /// praticamente zero: ela nao inventa erro onde nao ha.
    /// </summary>
    [Fact]
    public void NumPolinomioBaixoAEstimativaEhQuaseZero()
    {
        var polinomio = new Polinomio(1, 2, 3, 4);
        var estimativa = Kronrod.Estimar(polinomio.Avaliar, 0, 1);

        Assert.True(estimativa.ErroEstimado < 1e-14);
        Assert.Equal(polinomio.Integral(0, 1).Valor(), estimativa.Valor, 1e-14);
    }

    /// <summary>
    /// A soma dos erros dos pedacos usa o MODULO, e nao o sinal. Somar com sinal
    /// deixaria os erros se cancelarem e produziria uma estimativa otimista
    /// demais, que e o pior defeito possivel.
    /// </summary>
    [Fact]
    public void OsErrosDosPedacosSaoSomadosEmModulo()
    {
        var (_, f, de, ate, _) = Casos.Raiz();
        var composta = Kronrod.Composta(f, de, ate, 8);

        double soma = 0;
        var h = (ate - de) / 8;
        for (var i = 0; i < 8; i++) soma += Kronrod.Estimar(f, de + h * i, de + h * (i + 1)).ErroEstimado;

        Assert.Equal(soma, composta.ErroEstimado, 1e-15);
    }
}
