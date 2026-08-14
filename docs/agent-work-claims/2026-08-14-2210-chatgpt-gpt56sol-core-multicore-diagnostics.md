# Work claim — Core model-health multicore diagnostics

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260814-core-multicore`
- Registered: `2026-08-14T22:10:00+07:00`
- Baseline main SHA: `2f3e60cefabf05e9e8cb63ffacb0e6359d3a35df`
- Implementation branch: `agent/chatgpt-gpt56sol/core-multicore-diagnostics`
- Integration batch: `integration/20260814-core-multicore`

## Scope

Add bounded CPU parallelism to the pure-Core comprehensive model-health orchestration without moving BricsCAD/Teigha document, database, transaction, editor, entity, or other host-affine objects onto worker threads. Preserve the existing provider ordering, issue de-duplication, exception-to-health-issue mapping, and deterministic output.

Reserved surfaces:
- `src/QS3D.Core/Diagnostics/ComprehensiveModelHealthService.cs`
- focused Core smoke coverage for comprehensive model-health multicore parity/determinism; prefer a new dedicated smoke file if the harness supports it

Explicitly out of scope:
- BricsCAD/V25 adapter, document/database/transaction/editor threading;
- native geometry operations or CAD entity access from worker threads;
- unrelated diagnostics provider behavior changes;
- changing release/qualification evidence.

## Acceptance

- Runtime can use more than one CPU worker for independent comprehensive Core health providers when configured/available.
- Parallel work is bounded by an explicit maximum degree of parallelism derived from a safe default and supports a deterministic single-worker path.
- Provider results are collected into isolated slots; shared output/dedup state is merged only after workers complete.
- Output order and de-dup semantics match the current sequential provider order.
- Existing diagnostic data failures still become `HEALTH_PROVIDER_FAILED` for the correct provider; unexpected exceptions retain existing fail-fast semantics.
- Focused smoke coverage proves single-worker and multi-worker result parity and repeated deterministic ordering without wall-clock assertions.
- Source remains `netstandard2.0` compatible and no CAD API types are introduced into the parallel Core lane.
