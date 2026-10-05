using Conde.Integracao;

namespace Conde.Integracao.Medidor;

/// <summary>
/// As medidas. Grau de exatidao conferido contra a integral EXATA de
/// polinomios, e erro contra valores conhecidos: nenhuma depende de relogio.
/// </summary>
public static class Programa
{
    public static int Main(string[] argumentos)
    {
        var qual = argumentos.Length > 0 ? argumentos[0] : "tudo";
        switch (qual)
        {
            case "grau": Grau(); break;
            case "gauss": GaussContraCotes(); break;
            case "romberg": Rombergs(); break;
            case "adaptativa": Adaptativas(); break;
            case "dificeis": Dificeis(); break;
            case "erro": Estimativa(); break;
            case "tudo":
                Grau(); GaussContraCotes(); Rombergs(); Adaptativas(); Dificeis(); Estimativa();
                break;
            default:
                Console.Error.WriteLine("medidas: grau, gauss, romberg, adaptativa, dificeis, erro, tudo");
                return 1;
        }
        return 0;
    }

    /// <summary>O grau de exatidao de cada regra, contra a integral exata.</summary>
    private static void Grau()
    {
        Console.WriteLine("== o grau de exatidao de cada regra, contra a integral exata ==");
        Console.WriteLine($"{"regra",-16}{"pontos",9}{"grau medido",14}{"o obvio seria",16}");

        foreach (var regra in Regras.Todas())
        {
            var obvio = regra.PontosPorPedaco - 1;
            Console.WriteLine($"{regra.Nome,-16}{regra.PontosPorPedaco,9}" +
                              $"{Regras.GrauDeExatidao(regra),14}{obvio,16}");
        }

        Console.WriteLine();
        Console.WriteLine("a linha da regra de SIMPSON e a que surpreende. Ela ajusta uma PARABOLA aos");
        Console.WriteLine("tres pontos, entao o obvio seria grau dois, e ela e exata ate grau TRES. O");
        Console.WriteLine("erro do termo cubico se cancela por simetria, de graca");
        Console.WriteLine();
        Console.WriteLine("o mesmo acontece com a de Boole, que ajusta uma quartica e acerta ate grau");
        Console.WriteLine("cinco. Toda regra de Newton e Cotes com numero IMPAR de pontos ganha um");
        Console.WriteLine("grau de brinde, e o ponto medio e o caso extremo: com UM ponto so ele");
        Console.WriteLine("empata com o trapezio, que usa dois");
        Console.WriteLine();
        Console.WriteLine("e a de Simpson tres oitavos e a contramao: ela usa um ponto a mais que a de");
        Console.WriteLine("Simpson e tem o MESMO grau. Custa mais e nao compra exatidao nenhuma");
        Console.WriteLine();
        Console.WriteLine("a conferencia e contra a integral EXATA de polinomios, em fracao de inteiro");
        Console.WriteLine("grande. Em ponto flutuante 'zero' e um numero perto de zero, e nao daria");
        Console.WriteLine("para separar erro de metodo de erro de arredondamento");
        Console.WriteLine();
    }

    /// <summary>Gauss escolhe onde avaliar, e isso vale o dobro do grau.</summary>
    private static void GaussContraCotes()
    {
        Console.WriteLine("== Gauss e Legendre: escolher onde avaliar vale o dobro ==");
        Console.WriteLine($"{"pontos",8}{"grau medido",14}{"a teoria diz 2n-1",20}{"Newton e Cotes daria",22}");

        foreach (var pontos in new[] { 1, 2, 3, 4, 5, 6, 8, 10 })
            Console.WriteLine($"{pontos,8}{Gauss.GrauDeExatidao(pontos),14}{2 * pontos - 1,20}" +
                              $"{(pontos % 2 == 1 ? pontos : pontos - 1),22}");

        Console.WriteLine();
        Console.WriteLine("uma regra de n pontos tem 2n numeros livres: n posicoes e n pesos. As");
        Console.WriteLine("regras de Newton e Cotes FIXAM as posicoes em pontos igualmente espacados e");
        Console.WriteLine("sobram n liberdades; Gauss usa as 2n e chega ao dobro do grau");
        Console.WriteLine();
        Console.WriteLine("com cinco pontos, uma regra de Newton e Cotes acerta ate grau cinco e esta");
        Console.WriteLine("acerta ate NOVE. Com dez pontos, dezenove contra nove");
        Console.WriteLine();
        Console.WriteLine("os nos sao as raizes do polinomio de Legendre e nao tem formula fechada:");
        Console.WriteLine("eles sao achados por Newton, e sao irracionais. A regra nao tem como ser");
        Console.WriteLine("exata em aritmetica exata, so em ponto flutuante");
        Console.WriteLine();
    }

    /// <summary>A tabela de Romberg, e de onde ela tira os digitos.</summary>
    private static void Rombergs()
    {
        Console.WriteLine("== a tabela de Romberg: o erro em cada celula, para exp(x) de 0 a 1 ==");

        const double certo = Math.E - 1;
        var tabela = Romberg.Tabela(Math.Exp, 0, 1, 6);

        Console.Write($"{"pedacos",10}");
        for (var j = 0; j < 6; j++) Console.Write($"{"coluna " + j,14}");
        Console.WriteLine();

        for (var i = 0; i < 6; i++)
        {
            Console.Write($"{1 << i,10}");
            for (var j = 0; j <= i; j++) Console.Write($"{Math.Abs(tabela[i, j] - certo),14:E1}");
            Console.WriteLine();
        }

        Console.WriteLine();
        Console.WriteLine("a coluna zero e o trapezio puro, e ela melhora devagar: dobrar os pedacos");
        Console.WriteLine("divide o erro por quatro. A coluna cinco chega ao limite do double com");
        Console.WriteLine("trinta e dois pedacos");
        Console.WriteLine();
        Console.WriteLine("a ideia e boa demais para ser tao pouco conhecida. O erro do trapezio e");
        Console.WriteLine("proporcional a h ao quadrado, mais um termo em h a quarta, mais um em h a");
        Console.WriteLine("sexta. Dois trapezios com passos h e h/2 erram de maneiras RELACIONADAS, e");
        Console.WriteLine("combinando os dois na proporcao certa o termo de h ao quadrado some inteiro");
        Console.WriteLine();
        Console.WriteLine("e o resultado dessa combinacao e EXATAMENTE a regra de Simpson. Combinando");
        Console.WriteLine("de novo sai a de Boole. A tabela de Romberg e essa combinacao repetida, e");
        Console.WriteLine("cada coluna nova mata mais um termo do erro");
        Console.WriteLine();

        var trapezio = Romberg.PrimeiraExtrapolacao(Math.Exp, 0, 1, 8);
        var simpson = Regras.Simpson(Math.Exp, 0, 1, 16);
        Console.WriteLine($"  a primeira extrapolacao do trapezio: {trapezio:N15}");
        Console.WriteLine($"  a regra de Simpson com o mesmo passo: {simpson:N15}");
        Console.WriteLine($"  diferenca: {Math.Abs(trapezio - simpson):E1}");
        Console.WriteLine();
        Console.WriteLine("  nao e semelhanca: as duas formulas sao a mesma formula escrita de dois");
        Console.WriteLine("  jeitos");
        Console.WriteLine();
    }

    /// <summary>A quadratura adaptativa, e onde ela gasta.</summary>
    private static void Adaptativas()
    {
        Console.WriteLine("== a quadratura adaptativa: onde ela decide gastar ==");
        Console.WriteLine($"{"funcao",-22}{"erro",12}{"avaliacoes",13}{"pedacos",10}{"menor pedaco",16}");

        foreach (var (nome, f, de, ate, certo) in Casos.Todos())
        {
            var saida = Adaptativa.Integrar(f, de, ate);
            Console.WriteLine($"{nome,-22}{Math.Abs(saida.Valor - certo),12:E1}{saida.Avaliacoes,13:N0}" +
                              $"{saida.Pedacos,10:N0}{saida.MenorPedaco,16:E1}");
        }

        Console.WriteLine();
        Console.WriteLine("a coluna do menor pedaco e onde esta a dificuldade de cada funcao. Na");
        Console.WriteLine("exponencial ela para em tres centesimos; na raiz de x ela desce a 3,6e-15,");
        Console.WriteLine("apertando em volta do zero, onde a derivada e infinita");
        Console.WriteLine();

        Console.WriteLine("  e o pico estreito, com orcamentos diferentes:");
        Console.WriteLine($"  {"tolerancia",14}{"erro",12}{"avaliacoes",13}{"pedacos",10}");

        var (_, pico, picoDe, picoAte, picoCerto) = Casos.Pico();
        foreach (var tolerancia in new[] { 1e-4, 1e-8, 1e-12 })
        {
            var saida = Adaptativa.Integrar(pico, picoDe, picoAte, tolerancia);
            Console.WriteLine($"  {tolerancia,14:E0}{Math.Abs(saida.Valor - picoCerto),12:E1}" +
                              $"{saida.Avaliacoes,13:N0}{saida.Pedacos,10:N0}");
        }

        Console.WriteLine();
        Console.WriteLine("  pedir mais exatidao custa mais avaliacoes, e a conta e quase linear: a");
        Console.WriteLine("  adaptativa nao desperdica pedindo precisao onde ela ja tem");
        Console.WriteLine();
    }

    /// <summary>A estimativa de erro que a propria conta calcula.</summary>
    private static void Estimativa()
    {
        Console.WriteLine("== saber que a resposta esta certa sem conhecer a resposta ==");
        Console.WriteLine($"{"funcao",-22}{"erro estimado",16}{"erro real",14}{"honesta",10}{"exagero",12}");

        foreach (var (nome, f, de, ate, certo) in Casos.Todos())
        {
            var estimativa = Kronrod.Estimar(f, de, ate);
            Console.WriteLine($"{nome,-22}{estimativa.ErroEstimado,16:E1}" +
                              $"{Math.Abs(estimativa.Valor - certo),14:E1}" +
                              $"{(Kronrod.Honesta(estimativa, certo) ? "sim" : "NAO"),10}" +
                              $"{Kronrod.Exagero(estimativa, certo),12:E1}");
        }

        Console.WriteLine();
        Console.WriteLine("num problema de verdade a integral certa nao e conhecida, e dizer que o");
        Console.WriteLine("erro e 2,8e-14 nao e uma opcao: e preciso ESTIMAR o erro a partir da");
        Console.WriteLine("propria conta. A maneira usual e rodar duas regras de ordens diferentes e");
        Console.WriteLine("usar a diferenca entre elas");
        Console.WriteLine();
        Console.WriteLine("a coluna da honestidade e a unica que importa: uma estimativa que");
        Console.WriteLine("SUBESTIMA e pior que nenhuma, porque faz o programa parar de dividir o");
        Console.WriteLine("intervalo achando que ja chegou. Exagerar e so caro");
        Console.WriteLine();
        Console.WriteLine("e ela exagera bastante, de uma vez e meia a oito mil vezes. O motivo e");
        Console.WriteLine("simples: a diferenca entre as duas regras e dominada pelo erro da PIOR,");
        Console.WriteLine("que e muito maior que o da melhor, e e a melhor que vira resposta");
        Console.WriteLine();

        Console.WriteLine("  e em pedacos, que e como ela e usada de verdade:");
        Console.WriteLine($"  {"funcao",-22}{"pedacos",9}{"estimado",13}{"real",13}{"avaliacoes",13}");

        foreach (var (nome, f, de, ate, certo) in Casos.Todos())
        {
            foreach (var pedacos in new[] { 1, 16, 64 })
            {
                var estimativa = Kronrod.Composta(f, de, ate, pedacos);
                Console.WriteLine($"  {(pedacos == 1 ? nome : ""),-22}{pedacos,9}" +
                                  $"{estimativa.ErroEstimado,13:E1}{Math.Abs(estimativa.Valor - certo),13:E1}" +
                                  $"{estimativa.Avaliacoes,13:N0}");
            }
            Console.WriteLine();
        }

        Console.WriteLine("  os erros dos pedacos sao somados em MODULO e nao com sinal. Somar com");
        Console.WriteLine("  sinal deixaria os erros se cancelarem e produziria uma estimativa");
        Console.WriteLine("  otimista demais, que e o pior defeito possivel numa estimativa de erro");
        Console.WriteLine();
    }
    /// <summary>As funcoes em que as regras fixas sofrem.</summary>
    private static void Dificeis()
    {
        Console.WriteLine("== o pico estreito: quando a regra fixa nao encosta nele ==");

        var (_, f, de, ate, certo) = Casos.Pico();
        Console.WriteLine($"  a funcao sobe e desce numa faixa de um centesimo do intervalo");
        Console.WriteLine($"  a integral certa vale {certo:N10}");
        Console.WriteLine();
        Console.WriteLine($"{"avaliacoes",12}{"Simpson fixo",18}{"erro",12}{"Gauss de 5, composto",24}{"erro",12}");

        foreach (var pontos in new[] { 10, 20, 40, 80, 160, 320 })
        {
            var simpson = Regras.Simpson(f, de, ate, pontos);
            var gauss = Gauss.Composta(f, de, ate, 5, pontos / 5);
            Console.WriteLine($"{pontos,12}{simpson,18:N8}{Math.Abs(simpson - certo),12:E1}" +
                              $"{gauss,24:N8}{Math.Abs(gauss - certo),12:E1}");
        }

        Console.WriteLine();
        Console.WriteLine("com dez avaliacoes, a regra de Simpson devolve 0,136 para uma integral que");
        Console.WriteLine("vale 0,031: quatro vezes o valor certo. Nenhum dos dez pontos cai perto do");
        Console.WriteLine("pico, e ela esta integrando outra funcao sem saber");
        Console.WriteLine();
        Console.WriteLine("e com orcamento grande ela alcanca, porque os pontos acabam cobrindo o");
        Console.WriteLine("pico. Isso me corrigiu: eu ia escrever que a adaptativa ganha sempre no");
        Console.WriteLine("pico, e com 320 avaliacoes a regra fixa ja chega a 9e-7, que e da mesma");
        Console.WriteLine("ordem do que a adaptativa consegue com 243");
        Console.WriteLine();

        Console.WriteLine("  o caso em que a adaptativa ganha de verdade e a RAIZ DE X, cuja derivada");
        Console.WriteLine("  e infinita em zero:");
        Console.WriteLine();
        Console.WriteLine($"  {"avaliacoes",12}{"Simpson fixo",16}{"adaptativa",16}{"razao",12}");

        var adaptativa = Adaptativa.Integrar(Math.Sqrt, 0, 1);
        var fixa = Regras.Simpson(Math.Sqrt, 0, 1, adaptativa.Avaliacoes);
        var erroFixo = Math.Abs(fixa - 2.0 / 3);
        var erroAdaptativo = Math.Abs(adaptativa.Valor - 2.0 / 3);

        Console.WriteLine($"  {adaptativa.Avaliacoes,12:N0}{erroFixo,16:E1}{erroAdaptativo,16:E1}" +
                          $"{erroFixo / erroAdaptativo,12:E1}x");

        Console.WriteLine();
        Console.WriteLine("  com o MESMO numero de avaliacoes, a adaptativa erra sete ordens de");
        Console.WriteLine("  grandeza menos. A funcao e perfeitamente integravel; o que quebra as");
        Console.WriteLine("  regras fixas e que elas supoem que a funcao se parece com um polinomio, e");
        Console.WriteLine("  nenhuma potencia inteira se parece com raiz perto do zero");
        Console.WriteLine();
    }
}
