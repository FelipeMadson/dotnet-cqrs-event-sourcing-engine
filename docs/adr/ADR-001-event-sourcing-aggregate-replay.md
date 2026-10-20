# ADR-001: Event Sourcing Aggregate Replay vs Relational State Mutation

## Status
Accepted

## Context
In core enterprise banking and trading ledgers, mutating rows directly in a relational database (`UPDATE accounts SET balance = balance - 100`) destroys historical auditability. When state discrepancies occur, reconstructing the exact sequence of events, temporal states, and debugging transaction ordering becomes nearly impossible.

## Decision
We adopted **Event Sourcing** where state is fundamentally represented as an append-only stream of immutable domain events:
1. **Append-Only Store**: All state mutations are persisted as immutable domain events (`AccountOpenedEvent`, `FundsDepositedEvent`, `FundsTransferredEvent`).
2. **Deterministic Aggregate Replay**: Aggregate domain entities reconstruct their state at runtime by sequentially applying past events (`Apply(IDomainEvent @event)`).
3. **Temporal Querying & Auditing**: Any historical state at timestamp `T` or version `V` can be mathematically reconstructed by replaying the event stream up to that boundary.

## Consequences
- **Positive**: 100% complete and immutable audit trail; effortless retrospective debugging; zero relational lock contention on write paths.
- **Trade-off**: Requires periodic snapshotting (`SnapshotEnvelope`) for aggregates with thousands of events to keep replay latency bounded.
