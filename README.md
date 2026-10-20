# .NET 9 CQRS & Event Sourcing Engine with Transactional Outbox

[![CI](https://github.com/FelipeMadson/dotnet-cqrs-event-sourcing-engine/actions/workflows/ci.yml/badge.svg)](https://github.com/FelipeMadson/dotnet-cqrs-event-sourcing-engine/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET 9](https://img.shields.io/badge/.NET-9.0-purple.svg)](https://dotnet.microsoft.com/)
[![Architecture: Clean CQRS](https://img.shields.io/badge/Architecture-Clean%20CQRS-blue.svg)](#architecture)
[![Bilingual: English & Portuguese](https://img.shields.io/badge/Language-EN%20%7C%20PT--BR-blue.svg)](README.pt-BR.md)

Production-grade, enterprise-scale implementation of **CQRS (Command Query Responsibility Segregation)**, **Event Sourcing**, and the **Transactional Outbox Pattern** built on **.NET 9** and **C# 13**. Designed to eliminate distributed dual-writes and guarantee deterministic event replay with strict optimistic concurrency control.

Authored by **Felipe Madison** ([@FelipeMadson](https://github.com/FelipeMadson)).

[Versão em Português](README.pt-BR.md) · [Portfolio](https://felipemadson.github.io/portfolio/) · [Architecture Handbook Vol 09](https://github.com/FelipeMadson/software-engineering-handbook)

---

## Key Features

- **Event Sourced Aggregate Root**: State mutation occurs exclusively via immutable domain events (`When(IDomainEvent)` pattern), supporting zero-loss deterministic event replay.
- **Strict Optimistic Concurrency Control**: Append-only event streams guarded by expected version checks (`ConcurrencyException` on conflicts).
- **Transactional Outbox Pattern**: Atomic persistence of domain events alongside transactional outbox records to guarantee at-least-once asynchronous message delivery without dual-writes.
- **Dedicated Background Outbox Worker**: High-performance `IHostedService` background worker polling and dispatching unpublished events with exponential retry.
- **Asynchronous Read Model Projections**: Read-side materialization providing eventual consistency for sub-millisecond query performance.
- **Modern Minimal API Architecture**: Clean Architecture division (`Domain`, `Application`, `Infrastructure`, `Api`, `Tests`).

---

## Architectural Topology

```
                         ┌──────────────────────────────────────────────┐
                         │                  HTTP CLIENT                 │
                         └──────┬────────────────────────────────┬──────┘
                                │ POST /api/orders (Command)     │ GET /api/orders/{id} (Query)
                                ▼                                ▼
               ┌─────────────────────────────────┐   ┌─────────────────────────────────┐
               │         COMMAND HANDLER         │   │          QUERY HANDLER          │
               │   (Create / Pay / Ship Order)   │   │     (Read Model Projection)     │
               └────────────────┬────────────────┘   └────────────────▲────────────────┘
                                │                                     │
                                ▼                                     │
               ┌─────────────────────────────────┐                    │
               │          AGGREGATE ROOT         │                    │
               │  State = Replay(DomainEvents)   │                    │
               └────────────────┬────────────────┘                    │
                                │                                     │
                    ┌───────────┴───────────┐                         │
                    ▼                       ▼                         │
         ┌─────────────────────┐ ┌─────────────────────┐              │
         │     EVENT STORE     │ │ TRANSACTIONAL OUTBOX│              │
         │  Immutable Streams  │ │  Pending Dispatch   │              │
         └─────────────────────┘ └──────────┬──────────┘              │
                                            │                         │
                                            ▼                         │
                                 ┌─────────────────────┐              │
                                 │   OUTBOX WORKER     ├──────────────┘
                                 │ (At-Least-Once Pub) │ Events Materialize Read Model
                                 └─────────────────────┘
```

---

## Solution Structure

```
├── DotnetCqrsEventSourcing.sln
├── src/
│   ├── DotnetCqrs.Domain/           # Aggregates, Domain Events, Entities, Exceptions
│   ├── DotnetCqrs.Application/      # Commands, Queries, Handlers, Abstractions
│   ├── DotnetCqrs.Infrastructure/   # InMemoryEventStore, OutboxProcessor, Projections
│   └── DotnetCqrs.Api/              # Minimal API Endpoints, DI Configuration
└── tests/
    └── DotnetCqrs.Tests/            # xUnit Suite (Replay, Concurrency, Outbox, Projections)
```

---

## Quickstart

### Prerequisites
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

### Restore & Build
```bash
dotnet restore DotnetCqrsEventSourcing.sln
dotnet build DotnetCqrsEventSourcing.sln -c Release
```

### Run Tests
```bash
dotnet test DotnetCqrsEventSourcing.sln -c Release --verbosity normal
```

### Run API
```bash
dotnet run --project src/DotnetCqrs.Api
```

---

## API Endpoints

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/orders` | Place a new order (Generates `OrderPlacedEvent`) |
| `POST` | `/api/orders/{id}/pay` | Process payment with version guard (`OrderPaymentProcessedEvent`) |
| `POST` | `/api/orders/{id}/ship` | Ship order with tracking number (`OrderShippedEvent`) |
| `GET` | `/api/orders/{id}` | Reconstruct aggregate state from event stream |
| `GET` | `/api/orders/projections/summary` | Query materialized eventual-consistent read model |
| `GET` | `/api/outbox/status` | Monitor transactional outbox message queue metrics |

---

## License

This project is licensed under the [MIT License](LICENSE).
Authored by **Felipe Madison** ([@FelipeMadson](https://github.com/FelipeMadson)).
