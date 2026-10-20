# Motor de CQRS & Event Sourcing em .NET 9 com Transactional Outbox

[![CI](https://github.com/FelipeMadson/dotnet-cqrs-event-sourcing-engine/actions/workflows/ci.yml/badge.svg)](https://github.com/FelipeMadson/dotnet-cqrs-event-sourcing-engine/actions)
[![Licença: MIT](https://img.shields.io/badge/Licen%C3%A7a-MIT-yellow.svg)](LICENSE)
[![.NET 9](https://img.shields.io/badge/.NET-9.0-purple.svg)](https://dotnet.microsoft.com/)
[![Arquitetura: Clean CQRS](https://img.shields.io/badge/Arquitetura-Clean%20CQRS-blue.svg)](#arquitetura)
[![Bilingue: Inglês & Português](https://img.shields.io/badge/Idioma-EN%20%7C%20PT--BR-blue.svg)](README.md)

Implementação corporativa de nível de produção dos padrões **CQRS (Command Query Responsibility Segregation)**, **Event Sourcing** e **Transactional Outbox** desenvolvida em **.NET 9** e **C# 13**. Projetada para eliminar problemas de gravação dupla distribuída (*dual-writes*) e garantir reconstrução determinística de estado com controle estrito de concorrência otimista.

De autoria de **Felipe Madison** ([@FelipeMadson](https://github.com/FelipeMadson)).

[English Version](README.md) · [Portfólio](https://felipemadson.github.io/portfolio/) · [Manual de Engenharia Vol 09](https://github.com/FelipeMadson/software-engineering-handbook)

---

## Principais Recursos

- **Raiz de Agregação Orientada a Event Sourcing**: Mutações de estado ocorrem exclusivamente por meio de eventos imutáveis de domínio (padrão `When(IDomainEvent)`), viabilizando *replay* determinístico sem perdas.
- **Controle Estrito de Concorrência Otimista**: Fluxos de eventos somente-adição protegidos por validação de versão esperada (`ConcurrencyException` em cenários conflitantes).
- **Padrão Transactional Outbox**: Persistência atômica de eventos de domínio em conjunto com registros de mensagens de saída para assegurar entrega assíncrona *at-least-once* sem *dual-writes*.
- **Worker de Segundo Plano Dedicado**: Processador em background `IHostedService` com polling contínuo e publicação assíncrona com retry exponencial.
- **Projeções Assíncronas de Leitura**: Materialização de modelos de consulta que provê consistência eventual para respostas sub-milissegundo.
- **Arquitetura Limpa em Minimal APIs**: Separação clara de responsabilidades em `Domain`, `Application`, `Infrastructure`, `Api` e `Tests`.

---

## Topologia Arquitetural

```
                         ┌──────────────────────────────────────────────┐
                         │                 CLIENTE HTTP                 │
                         └──────┬────────────────────────────────┬──────┘
                                │ POST /api/orders (Comando)     │ GET /api/orders/{id} (Consulta)
                                ▼                                ▼
               ┌─────────────────────────────────┐   ┌─────────────────────────────────┐
               │        HANDLER DE COMANDO       │   │       HANDLER DE CONSULTA       │
               │   (Criar / Pagar / Despachar)   │   │     (Projeção Read Model)       │
               └────────────────┬────────────────┘   └────────────────▲────────────────┘
                                │                                     │
                                ▼                                     │
               ┌─────────────────────────────────┐                    │
               │       RAIZ DE AGREGAÇÃO         │                    │
               │  Estado = Replay(DomainEvents)  │                    │
               └────────────────┬────────────────┘                    │
                                │                                     │
                    ┌───────────┴───────────┐                         │
                    ▼                       ▼                         │
         ┌─────────────────────┐ ┌─────────────────────┐              │
         │     EVENT STORE     │ │ TRANSACTIONAL OUTBOX│              │
         │   Streams Imutáveis │ │  Disparo Pendente   │              │
         └─────────────────────┘ └──────────┬──────────┘              │
                                            │                         │
                                            ▼                         │
                                 ┌─────────────────────┐              │
                                 │    OUTBOX WORKER    ├──────────────┘
                                 │  (Entrega Garantida)│ Eventos Materializam Modelo de Leitura
                                 └─────────────────────┘
```

---

## Estrutura da Solução

```
├── DotnetCqrsEventSourcing.sln
├── src/
│   ├── DotnetCqrs.Domain/           # Agregados, Eventos de Domínio, Entidades, Exceções
│   ├── DotnetCqrs.Application/      # Comandos, Consultas, Handlers, Abstrações
│   ├── DotnetCqrs.Infrastructure/   # InMemoryEventStore, OutboxProcessor, Projeções
│   └── DotnetCqrs.Api/              # Endpoints Minimal API, Configuração de DI
└── tests/
    └── DotnetCqrs.Tests/            # Suíte xUnit (Replay, Concorrência, Outbox, Projeções)
```

---

## Execução Rápida

### Pré-requisitos
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

### Restaurar e Compilar
```bash
dotnet restore DotnetCqrsEventSourcing.sln
dotnet build DotnetCqrsEventSourcing.sln -c Release
```

### Executar Testes
```bash
dotnet test DotnetCqrsEventSourcing.sln -c Release --verbosity normal
```

### Executar API
```bash
dotnet run --project src/DotnetCqrs.Api
```

---

## Licença

Este projeto é licenciado sob a [Licença MIT](LICENSE).
De autoria de **Felipe Madison** ([@FelipeMadson](https://github.com/FelipeMadson)).
