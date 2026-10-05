# integracao

Quadratura numérica do zero em C# e .NET 8: Newton-Cotes, Gauss-Legendre,
Romberg e adaptativa. O juiz é a integral **exata** de polinômios, em frações de
inteiro grande.

```
$ dotnet medidor.dll grau

regra              pontos   grau medido   o obvio seria
ponto medio             1             1               0
trapezio                2             1               1
Simpson                 3             3               2
Simpson 3/8             4             3               3
Boole                   5             5               4
```

A linha da regra de **Simpson** é a que surpreende. Ela ajusta uma **parábola**
aos três pontos, então o óbvio seria grau dois, e ela é exata até grau **três**: o
erro do termo cúbico se cancela por simetria, de graça.

Toda regra de Newton-Cotes com número **ímpar** de pontos ganha um grau de
brinde, e o ponto médio é o caso extremo: com **um** ponto só ele empata com o
trapézio, que usa dois.

E a de Simpson três oitavos é a contramão: um ponto a mais que a de Simpson, o
mesmo grau. Custa mais e não compra exatidão nenhuma.

## Por que o juiz é exato

A pergunta é "esta regra é exata até que grau?", e em ponto flutuante "zero" é um
número perto de zero: não dá para separar erro de método de erro de
arredondamento. Com a integral exata a resposta é binária.

## Escolher onde avaliar vale o dobro

```
$ dotnet medidor.dll gauss

  pontos   grau medido   a teoria diz 2n-1   Newton e Cotes daria
       3             5                   5                      3
       5             9                   9                      5
      10            19                  19                      9
```

Uma regra de n pontos tem 2n números livres: n posições e n pesos. As regras de
Newton-Cotes **fixam** as posições em pontos igualmente espaçados e sobram n
liberdades; Gauss usa as 2n e chega ao dobro do grau.

Os nós são as raízes do polinômio de Legendre e não têm fórmula fechada: são
achados por Newton, e são irracionais. A regra não tem como ser exata em
aritmética exata, só em ponto flutuante.

## Romberg: de onde ele tira os dígitos

```
$ dotnet medidor.dll romberg

   pedacos      coluna 0      coluna 1      coluna 2      coluna 3      coluna 4
         1      1.4E-001
         2      3.6E-002      5.8E-004
         4      8.9E-003      3.7E-005      8.6E-007
         8      2.2E-003      2.3E-006      1.4E-008      3.4E-010
        16      5.6E-004      1.5E-007      2.2E-010      1.3E-012      3.4E-014
        32      1.4E-004      9.1E-009      3.4E-012      4.9E-015      2.2E-016
```

A coluna zero é o trapézio puro: dobrar os pedaços divide o erro por **quatro**. É
dessa relação que a extrapolação tira os dígitos.

O erro do trapézio é proporcional a h², mais um termo em h⁴, mais um em h⁶. Dois
trapézios com passos h e h/2 erram de maneiras **relacionadas**, e combinando os
dois na proporção certa o termo de h² some inteiro. O resultado dessa combinação
é **exatamente** a regra de Simpson, e um teste confere a igualdade em quatro
funções e quatro tamanhos de passo. Combinando de novo sai a de Boole.

## O que as medidas me corrigiram

**A adaptativa não ganha sempre no pico.** Eu ia escrever que a regra fixa não
encosta num pico estreito e que a adaptativa resolve.

```
$ dotnet medidor.dll dificeis

  avaliacoes      Simpson fixo        erro   Gauss de 5, composto        erro
          10        0.13565967    1.0E-001             0.02067362    1.0E-002
          20        0.04039434    9.4E-003             0.03015720    8.6E-004
         320        0.03101508    9.0E-007             0.03101618    2.0E-007
```

Com dez avaliações ela devolve 0,136 para uma integral que vale 0,031: quatro
vezes o valor certo, porque nenhum dos dez pontos cai perto do pico e ela está
integrando outra função sem saber.

Mas com orçamento grande ela alcança. Com 320 avaliações a regra fixa já chega a
9e-7, que é da mesma ordem do que a adaptativa consegue com 243.

**O caso em que a adaptativa ganha de verdade é a raiz de x**, cuja derivada é
infinita em zero:

```
  avaliacoes      Simpson fixo      adaptativa       razao
       4,419          2.8E-007        2.8E-014    9.9E+006x
```

Com o **mesmo** número de avaliações, ela erra sete ordens de grandeza menos, e
aperta os pedaços até 3,6e-15 em volta do zero. A função é perfeitamente
integrável; o que quebra as regras fixas é que elas supõem que a função se parece
com um polinômio, e nenhuma potência inteira se parece com raiz perto do zero.

**E "acerta função lisa" esconde seis ordens de grandeza.** Eu exigi num teste
erro abaixo de um milionésimo para todas as regras numa função lisa, e o trapézio
no seno dá 1,6 milionésimos: ele passa raspando de uma exigência que Simpson
supera por um fator de um milhão, com as mesmas mil avaliações.

## As peças

| arquivo | o que faz |
| --- | --- |
| `Exata.cs` | a fração de inteiro grande e a integral exata de polinômios |
| `Regras.cs` | ponto médio, trapézio, Simpson, 3/8 e Boole, com o grau medido |
| `Gauss.cs` | os nós de Legendre por Newton, e a regra composta |
| `Romberg.cs` | a extrapolação, a adaptativa e os casos de teste |

## Como rodar

```
dotnet test testes/Integracao.Testes/Integracao.Testes.csproj -c Release
dotnet run --project ferramentas/Medidor/Medidor.csproj -c Release -- tudo
```

As medidas aceitam `grau`, `gauss`, `romberg`, `adaptativa`, `dificeis` e `tudo`.

## Licença

MIT.
