# ADR-002: Transactional Outbox Pattern for Asynchronous Read-Model Projections

## Status
Accepted

## Context
In a CQRS architecture, write aggregates produce domain events, while read models (projections) maintain denormalized read-optimized views (e.g. for customer dashboards). Direct dual-writing to both the event store and message broker/read store in the same request boundary introduces dual-write hazards: if the broker publish fails after database commit, the system enters an inconsistent state.

## Decision
We implemented the **Transactional Outbox Pattern**:
1. **Atomic Local Commit**: Domain events and corresponding outbox messages are committed atomically in the same local database transaction.
2. **Asynchronous Polling Dispatcher**: A background worker polls uncommitted outbox entries, publishes them to the projection dispatcher, and marks them as dispatched upon acknowledgment.
3. **Idempotent Consumer Projections**: Read-model projectors maintain an event version checkpoint, ensuring safe at-least-once message delivery without duplicates.

## Consequences
- **Positive**: Strict eventual consistency guaranteed across write models and read projections without distributed two-phase commit (2PC) transactions.
- **Trade-off**: Slight projection lag (sub-second) between write commit and read model queryability.
