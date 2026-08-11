# Project semantic mutation journal and rollback scope

Status: `SOURCE_IMPLEMENTED` for CAD-independent `ProjectState` mutation boundaries.

`ProjectSemanticMutationExecutor` provides a reusable **semantic-only** operation scope with `ProjectStateSnapshot` rollback and a detached journal.

## Phases

The journal records deterministic ordered phases while capacity remains available:

- `Planned` — semantic snapshot captured;
- `Running` — mutation delegate executing;
- `Validating` — optional pre-commit validation executing;
- `Committed` — semantic operation accepted;
- `RollingBack` — mutation/validation failed and restore started;
- `RolledBack` — captured ProjectState restored;
- `RollbackFailed` — restore itself failed; caller receives an aggregate failure.

The journal is detached from `ProjectState`: rollback does not erase diagnostic phase evidence, and writing the journal itself does not change project audit/metadata or `ChangeVersion`.

The journal is also deliberately non-authoritative. Its bounded 256-entry capacity protects diagnostics from unbounded growth, but diagnostic saturation must never fail, roll back, skip validation, or otherwise change a semantic mutation outcome. Executor phase writes are therefore best-effort once the journal is full; normal unsaturated operations still record the complete ordered phase sequence.

## Pre-commit validation

A caller may provide a pre-commit validation callback after the mutation delegate returns but before `Committed` is recorded.

This is useful for deterministic fault injection and cross-checks that need to observe the fully mutated semantic state. If validation throws, the executor restores the captured semantic project. Diagnostic saturation does not bypass this validation callback.

Smoke coverage deliberately runs a complete interchange AppendOnly+provenance mutation and then injects a validation failure. The outer scope must restore elements, metadata/provenance, audits, `UpdatedUtc` and `ChangeVersion` to the pre-operation snapshot. Separate saturation coverage fills one detached journal to its cap and verifies that the next semantic mutation still commits normally.

## Rollback failure

If the mutation fails and `ProjectStateSnapshot.Restore` also fails, the executor records `RollbackFailed` when possible and throws an `InvalidOperationException` whose inner `AggregateException` retains both failures. It never hides the original operation failure behind a restore exception.

## Native CAD boundary

This executor **does not roll back native DWG** entities, BricsCAD transactions, UI state, external files, certificate stores, network calls or other side effects outside `ProjectState`.

A workflow that mutates native CAD must still use a real BricsCAD transaction or a durable compensation/recovery design. Wrapping native operations in this semantic executor does not make them atomic.

Native failure injection and transaction qualification remain `LOCAL_ONLY` on licensed BricsCAD V25.

## Intended users

The primitive is appropriate for future Core-only orchestration where one outer semantic scope should protect several already-validated semantic steps, including interchange composition, documentation catalog changes and other deterministic project mutations.

It should not replace feature-specific validation, ownership checks or native transaction boundaries.