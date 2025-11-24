Firebird 2.5 - Character Sets e Collates
 
 Categoria: NÃO CATEGORIZADO
Fonte: http://asfernandes.blogspot.com.br/2014/01/artigo-novidades-do-firebird-25_5.html

No Firebird, diferentes tabelas ou até mesmo diferentes colunas de uma mesma tabela podem usar diferentes character sets e collates. Isto permite uma grande flexibilidade mas pode causar problemas caso o desenvolvedor ou DBA não se atente às necessidades da aplicação. Por isso, um banco de dados pode ter um character set default, que é automaticamente usado quando não é especificado um character set durante a definição de uma coluna de tipo string. Algo semelhante não existia para collates, sendo que o collate default de cada character set é o modo de comparação binária dos bytes que compõem uma string.

Na nova versão foi introduzido o comando ALTER CHARACTER SET, permitindo a alteração do collate padrão de um character set. Além do comando ALTER CHARACTER SET, agora o collate padrão do character set padrão do banco pode ser definido no momento da criação do banco de dados. A listagem 1 mostra a criação de um banco de dados usando o character set padrão WIN1252 e definindo seu collate padrão para WIN_PTBR. Em seguida o collate padrão do character set UTF8 é alterado para UNICODE_CI_AI.


Listagem 1. Exemplos de alteração de collate padrão.


CREATE DATABASE 'TEST.FDB'
  DEFAULT CHARACTER SET WIN1252
  COLLATION WIN_PTBR;

ALTER CHARACTER SET UTF8
  SET DEFAULT COLLATION UNICODE_CI_AI;


A listagem 2 mostra o banco operando com o character set default WIN1252 e o collate WIN_PTBR (case-insensitive).

Listagem 2. Comparação de strings usando o collate WIN_PTBR.

CREATE TABLE PESSOAS (NOME VARCHAR(20));

INSERT INTO PESSOAS VALUES ('Fulano');
INSERT INTO PESSOAS VALUES ('Beltrano');

-- Localiza Fulano (= FULANO)
SELECT * FROM PESSOAS WHERE NOME = 'FULANO';

Outra necessidade comumente encontrada em aplicações é a gravação de códigos alfanuméricos. Até o Firebird 2.1 a ordenação de campos do tipo string era sempre feita no modo de comparação de texto. Isto quer dizer que um código “A10” é listado antes de “A2” caso ordenado por esta coluna. A listagem 3 mostra um exemplo desta situação.

Listagem 3. Ordenação de códigos alfanuméricos usando o collate UNICODE.

CREATE TABLE DOCUMENTOS (
  CODIGO VARCHAR(10) CHARACTER SET UTF8 COLLATE UNICODE
);

INSERT INTO DOCUMENTOS VALUES ('A1');
INSERT INTO DOCUMENTOS VALUES ('A2');
INSERT INTO DOCUMENTOS VALUES ('A10');
INSERT INTO DOCUMENTOS VALUES ('A11');
INSERT INTO DOCUMENTOS VALUES ('A100');
INSERT INTO DOCUMENTOS VALUES ('B1');
INSERT INTO DOCUMENTOS VALUES ('B10');

-- Resultado: A1, A10, A100, A11, A2, B1, B10
SELECT CODIGO FROM DOCUMENTOS ORDER BY CODIGO;


O Firebird 2.5 permite que collates sejam configuráveis, e uma das opções do collate UNICODE permite que números sejam ordenados por ordem numérica. Alistagem 4 mostra a criação do collate UNICODE_NUM com o uso da opção NUMERIC-SORT e a ordenação usando este collate.

Listagem 4. Ordenação usando a opção NUMERIC-SORT do collate UNICODE.

CREATE COLLATION UNICODE_NUM FOR UTF8 FROM UNICODE 'NUMERIC-SORT=1';

CREATE TABLE DOCUMENTOS2 (
  CODIGO VARCHAR(10) CHARACTER SET UTF8 COLLATE UNICODE_NUM
);

INSERT INTO DOCUMENTOS2 SELECT * FROM DOCUMENTOS;

-- Resultado: A1, A2, A10, A11, A100, B1, B10
SELECT CODIGO FROM DOCUMENTOS2 ORDER BY CODIGO;


Outra novidade da versão 2.5 é o collate UNICODE_CI_AI (para o character set UTF8), variação do collate UNICODE pré-configurado para desconsiderar diferenças de acentos e maiúsculas/minúsculas. O collate UNICODE_CI_AI funciona de maneira similar ao WIN_PTBR e PT_BR, mas aceita o conjunto completo de caracteres Unicode.
=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-==-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=--=-=-=-=-=
Fonte: http://www.firebase.com.br/artigo.php?id=1
Como fazer para utilizar caracteres acentuados (português) e obter ordenação correta nos índices ?
Data da última atualização: 03/12/2010

Um charset define como o Firebird traduzirá um caractere em seu respectivo valor numérico, na tabela de caracteres (ou vice-versa), bem como quais “símbolos” estarão disponíveis para serem utilizados em campos do tipo string. Dependendo do charset escolhido, o símbolo associado ao valor “x” pode ser totalmente diferente do símbolo associado ao mesmo valor, em outro charset.

A definição do charset correto para o banco de dados, ou mesmo para os campos “string” de alguma tabela, implica diretamente nos “tipos” de caracteres (símbolos) que serão aceitos durante a manipulação dos dados nessa tabela.

Falando especificamente da língua portuguesa, existem inúmeros caracteres “especiais”, como o ç (cedilha), além das diversas formas de acentos que podem ser aplicados (agudo, circunflexo, crase, etc) a um caractere. Se não usarmos o charset correto no banco de dados, haverá grandes chances de obtermos mensagens de erro durante a manipulação dos dados. A mais famosa dessas mensagens é a "arithmetic exception, numeric overflow, or string truncation - Cannot transliterate character between character sets".

Cada charset disponível no Firebird (a exceção do NONE) possui pelo menos uma collation associada. A collation instrui o servidor em como ele deve tratar os caracteres no momento de fazer uma ordenação ou comparação entre eles.

Antes da versão 2.0, não existia no Firebird uma collation que permitisse fazer buscas, comparações ou indexações sem levar em conta caracteres acentuados ou especiais, ou mesmo a caixa dos caracteres. O Firebird 2.0 trouxe duas collations case/accent-insensitive para o Brasil: PT_BR (para o charset ISO8859_1) eWIN_PTBR (para o charset WIN1252). Com essas collations, o Firebird tratará, por exemplo, José = Jose = JOSE = JOSÉ = jose = JoSé, etc.

Tanto o charset quanto a collation pode ser definido em nível de campo, ou seja, em uma mesma tabela do banco de dados, podemos ter campos usando charsetse collations diferentes. A listagem abaixo apresenta uma tabela chamada TESTE, com campos usando diferentes charsets e collations.

CREATE TABLE  TESTE (
       CAMPOA   VARCHAR(50) CHARACTER SET WIN1252 COLLATE WIN_PTBR,
       CAMPOB   VARCHAR(50) CHARACTER SET ISO8859_1 COLLATE PT_BR,
       CAMPOC   VARCHAR(50) CHARACTER SET ASCII,
       CAMPOD   VARCHAR(50) CHARACTER SET CYRL COLLATE DB_RUS
   );
Dica:
Em geral, a conversão entre dois charsets pelo Firebird utiliza o UNICODE como “ponte”, sendo assim, a conversão do charset win1252 para o iso8859_1 é realizada da seguinte forma:
win1252 → UNICODE → iso8859_1

O charset também pode ser definido globalmente, para todo o banco de dados. Isso é feito no momento da criação do banco, como no exemplo abaixo:

CREATE DATABASE 'c:\livro\filmes.fdb'
   USER 'SYSDBA'  PASSWORD 'masterkey'
   PAGE_SIZE 4096
   DEFAULT CHARACTER SET  WIN1252;
Quando um charset padrão é definido no banco de dados, todos os campos das tabelas desse BD que não tiverem o charset explicitamente definido no momento da criação do campo, assumirão automaticamente o charset padrão do banco. A partir do Firebird 2.5, também é possível definir o COLLATE padrão para o banco:

create  database <file name>
    [ page_size <page size> ]
       [ length = <length> ]
       [ user <user name> ]
       [ password <user password> ]
       [ set names <charset name> ]
       [ default character set <charset  name>
       [[ collation  <collation name> ]]
       [ difference file <file name> ]
Quando o banco de dados possui um charset padrão diferente de NONE ou OCTETS, ou mesmo quando temos um charset especificado em algum campo de qualquer tabela desse banco de dados, devemos informar antes de conectá-lo, qual o charset que será utilizado nessa conexão. O charset da conexão deve ser definido de acordo com a codificação usada na aplicação cliente que está acessando os dados. Isso é necessário para que o Firebird saiba como “traduzir” o conteúdo textual dos campos que tem charset definido para a codificação utilizada pela aplicação cliente. 
O comando utilizado para especificar qual será o charset da conexão é o SET NAMES. Abaixo podemos ver um exemplo (executado no isql):

SET NAMES WIN1252; -- Configura o charset da conexão para WIN1252
CONNECT 'localhost:c:\livro\filmes.fdb' USER 'SYSDBA' PASSWORD 'masterkey';

Importante: A não definição do charset da conexão, quando esta é feita em bancos de dados contendo campos com charset definidos, pode gerar erros do tipo “Cannot transliterate character between character sets” quando algum select for executado. Do mesmo modo, a definição de um charset da conexão que não seja “compatível” com os charsets utilizados no banco de dados, poderá também gerar esse erro, ou mesmo causar a apresentação de caracteres “estranhos” na aplicação. Os erros são comuns nas situações onde a informação armazenada em um campo definido com o charset Y contém algum caractere/símbolo que não esteja representado no charset X (usado na conexão).

Caso o leitor esteja utilizando o Delphi para gerar a aplicação cliente, o charset da conexão pode ser definido no componente responsável por realizar a conexão com o banco de dados, que varia de acordo com a tecnologia de acesso utilizada. A tabela 1 mostra os principais componentes de acesso, e a propriedade que deve ser usada para definir o charset da conexão.

Pacote

Componente de conexão

Propriedade

IBO

TIB_DataBase, TIBODatabase, TIB_Connection

CharSet

dbExpress

TSQLConnection

Params/ServerCharset

BDE

TDatabase

Params/LANGDRIVER

IBX

TIB_DataBase

Params/lc_ctype

Zeos

TZConnection

Deve ser utilizado o seguinte código, antes da conexão:
ZConnection1.Properties.Add ('codepage=WIN1252'); 
* Substitua WIN1252 pelo charset desejado

Tabela 1. Propriedades para definir o charset, de acordo com o componente de acesso

Dica: As collations cujo nome começam com ISO utilizam ordenação do tipo “dictionary sort”, onde os espaços e outros caracteres de pontuação são tratados de forma diferenciada, gerando muitas vezes uma ordenação “estranha”, por exemplo, "AA" viria antes de "A B".

A seguir veremos alguns exemplos utilizando collations. Depois de criada a tabela TESTE, vamos inserir o seguinte registro:

insert into teste (campoa, campob, campoc, campod) 
values ('José João','José Joao','Jose Joao','Jose Joao')

Com isso, teremos na tabela apenas um registro, conforme indicado abaixo:

CAMPOA

CAMPOB

CAMPOC

CAMPOD

José João

José Joao

Jose Joao

Jose Joao

Para simplificar o entendimento de como uma collation afeta a comparação das strings, estude os exemplos abaixo, analisando seus resultados:

select count(*) 
   from teste
   where campoA = 'Jose Joao'
Resultado = 1, pois o CampoA está definido com a collation WIN_PTBR, que é case/accent insensitive.

select count(*) 
   from teste
   where campoB = 'Jose Joao'
Resultado = 1, pois o CampoB está definido com a collation PT_BR, que é case/accent insensitive.

select count(*) 
   from teste
   where campoB = campoA
Resultado = 1, pois o Firebird faz a “tradução” entre os charsets WIN1252 e ISO8859_1 utilizando as collations definidas (que são case/accent insensitive).

Collations e ordenações

Como vimos anteriormente, a collation determina a forma que as strings são comparadas, e também ordenadas. Quando usamos order by em selects, podemos determinar qual collation usar para a ordenação dos campos, bastando indicá-las depois do nome do campo em questão. Veja o exemplo:

CREATE TABLE  CLIENTES (
       CODIGO          BIGINT NOT NULL,
       NOME            VARCHAR(100) NOT  NULL COLLATE WIN_PTBR,
       ENDERECO        VARCHAR(50) NOT NULL,
       CIDADE          VARCHAR(50) NOT NULL,
       UF              VARCHAR(50),
       CEP             INTEGER,
       PAIS            VARCHAR(50) NOT NULL,
   );
select *
   from clientes
   order by nome, cidade collate win_ptbr
No exemplo anterior, os dados seriam ordenados por nome usando a collation win_ptbr, pois é a que foi definida no momento de criação do campo, e sub-ordenado pelo campo cidade. Se consultarmos a DDL da tabela clientes, veremos que o campo cidade não tem collation definida, portanto, para seguir o mesmo esquema de ordenação do campo nome, precisamos especificar que a collation que queremos usar para a ordenação do campo cidade é a win_ptbr.
Poderíamos também “misturar” collations que pertencem ao mesmo charset, mas que não tem muita semelhança entre si, por exemplo:

select *
   from clientes
   order by nome collate pxw_span, cidade collate win_ptbr
No exemplo acima, o campo nome estaria sendo ordenado seguindo o critério de ordenação pxw_span, ou seja, o modelo espanhol, enquanto que o campo cidadecontinuaria sendo ordenado pelo padrão “brasileiro”.

Dica: Até o Firebird 1.5, quando se usa a função UPPER para converter o conteúdo de um campo para maiúsculo, e esse campo fazia uso da collation binária padrão do charset, apenas os caracteres pertencentes à tabela ASCII eram convertidos. Portanto, caracteres como á, é, ó, etc. não eram convertidos em suas formas maiúsculas. A partir do Firebird 2.0, a conversão é feita corretamente.

Conclusão
Para que o Firebird não faça distinção entre letras maiúsculas e minúsculas, e também entre letras acentuadas e não acentuadas, devemos usar uma collation case/accent-insensitive. Para isso, em bancos de dados armazenando informações em língua portuguesa, aconselho o uso do CHARSET WIN1252 no Banco de Dados, e do COLLATE WIN_PTBR (nos campos onde não se deseja ter distinção).