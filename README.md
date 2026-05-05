# MigracaoDados

Ferramenta interna para apoiar processos de migracao de dados em ambiente Windows corporativo.

## Objetivo

O projeto nasce como uma aplicacao desktop WPF em .NET 8 LTS, com foco em uso interno, estabilidade e evolucao incremental. A arquitetura separa a interface da regra de negocio para permitir crescimento do sistema e uma possivel interface futura em Avalonia ou outra tecnologia cross-platform.

## Arquitetura

```text
MigracaoDados
|-- src
|   |-- MigracaoDados.Domain
|   |-- MigracaoDados.Application
|   |-- MigracaoDados.Infrastructure
|   |-- MigracaoDados.Avalonia
|   `-- MigracaoDados.Wpf
`-- tests
    |-- MigracaoDados.Application.Tests
    `-- MigracaoDados.Domain.Tests
```

- `Domain`: regras de negocio puras, entidades e value objects.
- `Application`: casos de uso, contratos e orquestracao da aplicacao.
- `Infrastructure`: implementacoes de acesso a arquivos, banco, APIs e recursos externos.
- `Wpf`: interface desktop, ViewModels, configuracao de Host, DI e logging.
- `Avalonia`: interface futura/opcional preparada para reaproveitar a camada `Application`.
- `tests`: testes automatizados das camadas internas.

## Estado Atual

A base atual ja possui um primeiro fluxo real de MVP: validacao de CSV de Contrato usando o layout oficial em XLSX como fonte de regras. A UI WPF permite selecionar um arquivo `.csv`, executar a validacao e visualizar erros com linha, coluna, valor informado, tipo do erro e mensagem.

O layout padrao esta em:

```text
Template Layout XLSX/Contrato.xlsx
```

O arquivo de layout deve possuir as colunas:

```text
ID; Descricao; Mnemonico; Obrigatoriedade; Tipo; Tamanho; Formato
```

Tipos suportados nesta versao:

- `Text`
- `Numeric`
- `Integer`
- `Decimal`
- `Date`
- `Boolean`

Validacoes suportadas:

- arquivo vazio ou sem cabecalho;
- coluna esperada ausente;
- coluna extra nao prevista no schema;
- ordem de colunas diferente da ordem do `ID` no layout;
- campo obrigatorio vazio;
- tipo invalido.
- tamanho maior que o permitido no layout.

## Como Validar

```powershell
dotnet build MigracaoDados.slnx
dotnet test MigracaoDados.slnx
```

## Proximos Passos

1. Permitir escolher o tipo de migracao/layout pela UI.
2. Adicionar preview de dados validos antes da importacao.
3. Definir contratos de importacao SQL Server.
4. Implementar importacao SQL Server com transacao e rollback.
5. Evoluir historico, auditoria e sugestoes de correcao.
