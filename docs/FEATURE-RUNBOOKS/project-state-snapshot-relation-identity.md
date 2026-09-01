# Project State Snapshot Relation Identity Integrity

## Scope

This Core-only contract keeps `ProjectStateSnapshot` relation text compatible with the canonical QSDB persistence boundary while preserving rollback/repair semantics. It applies to mutable `ProjectElement.SourceHandles` and `ProjectElement.DependsOn` values observed by snapshot capture, detached-copy creation, and rollback materialization.

## Invariant

Before a snapshot retains or republishes an element relation list, every relation identity must be:

- nonblank;
- free of leading/trailing whitespace;
- free of control characters and valid XML text.

The existing nested cardinality ceiling of 10,000 entries is enforced before relation identity validation. Snapshot validation must fail closed without mutating the source project or element persistence state.

Canonical XML-safe Unicode relation identities are preserved byte-semantically; snapshot code does not trim or otherwise normalize valid values.

Duplicate relation identities are intentionally preserved by the snapshot layer. They are representable transient state used by repair workflows such as Room Finish dependency synchronization, which must be able to capture rollback state before canonicalizing duplicate dependencies. Canonical persistence remains the final uniqueness gate and continues to reject duplicate persisted `<h>` and `<d>` identities.

## Why this is required

The QSDB XML schema validator rejects malformed/non-canonical persisted relation text and duplicate persisted identities. Snapshot capture must reject relation text that cannot safely cross the XML persistence boundary, but it must not make repairable in-memory duplicate state impossible to snapshot for rollback. This keeps transactional repair workflows usable while preserving fail-closed persistence.

## Deterministic validation

Run the Core smoke suite and focused source guard:

```text
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
python scripts/preflight-project-state-snapshot-relation-identity.py
```

The snapshot smoke covers padded, blank, control-bearing and malformed XML/UTF-16 values for both relation collections, valid Unicode round-trip, and exact preservation of repairable case-insensitive duplicates. Existing Room Finish dependency-repair smoke proves duplicate dependency state can still be captured and repaired to one canonical Room dependency.

## Runtime boundary

This contract is deterministic Core persistence/model-lifecycle correctness. Licensed BricsCAD execution is not required and remote/static validation must not be reported as `LOCAL_PASS`.
