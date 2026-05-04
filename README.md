# MigracaoDados

Ferramenta interna para apoiar processos de migracao de dados em ambiente Windows corporativo.

## Objetivo

O projeto nasce como uma aplicacao desktop WPF em .NET 8 LTS, com foco em uso interno, estabilidade e evolucao incremental. A arquitetura separa a interface da regra de negocio para permitir crescimento do sistema e uma possivel interface futura em Avalonia ou outra tecnologia cross-platform.

## Arquitetura

```text
MigracaoDados
├── src
│   ├── MigracaoDados.Domain
│   ├── MigracaoDados.Application
│   ├── MigracaoDados.Infrastructure
│   ├── MigracaoDados.Avalonia
│   └── MigracaoDados.Wpf
└── tests
    ├── MigracaoDados.Application.Tests
    └── MigracaoDados.Domain.Tests
```

- `Domain`: regras de negocio puras, entidades e value objects.
- `Application`: casos de uso, contratos e orquestracao da aplicacao.
- `Infrastructure`: implementacoes de acesso a arquivos, banco, APIs e recursos externos.
- `Wpf`: interface desktop, ViewModels, configuracao de Host, DI e logging.
- `Avalonia`: interface futura/opcional preparada para reaproveitar a camada `Application`.
- `tests`: testes automatizados das camadas internas.

## Estado Atual

A base inicial ja possui WPF com Host Builder, injecao de dependencia, `appsettings.json`, Serilog e um primeiro caso de uso demonstrativo chamado pela ViewModel.

## Proximos Passos

1. Definir o primeiro fluxo real de migracao.
2. Modelar entradas, saidas, validacoes e resultado da migracao.
3. Criar contratos em `Application` e implementacoes em `Infrastructure`.
4. Evoluir a UI para selecao de origem, execucao, progresso e resumo.
5. Cobrir regras principais com testes automatizados.
