# Project State Snapshot Relation Identity Integrity

## Scope

This Core-only contract keeps `ProjectStateSnapshot` relation state compatible with the canonical QSDB persistence boundary. It applies to mutable `ProjectElement.SourceHandles` and `ProjectElement.DependsOn` values observed by snapshot capture, detached-copy creation, and rollback materialization.

## Invariant

Before a snapshot retains or republishes an element relation list, every relation identity must be:

- nonblank;
- free of leading/trailing whitespace;
- free of control characters and valid XML text;
- unique under the repository's ordinal-ignore-case identity semantics.

The existing nested cardinality ceiling of 10,000 entries is enforced before relation identity validation. Snapshot validation must fail closed without mutating the source project or element persistence state.

Canonical XML-safe Unicode relation identities are preserved byte-semantically; snapshot code does not trim or otherwise normalize valid values.

## Why this is required

The QSDB XML schema validator already rejects non-canonical or duplicate persisted `<h>` source handles and `<d>` dependency ids. `SourceHandles` and `DependsOn` are publicly mutable collections, so snapshot capture must enforce the same identity boundary before it can carry state into rollback or later persistence. Otherwise an in-memory snapshot can accept state that canonical persistence subsequently rejects.

## Deterministic validation

Run the Core smoke suite and focused source guard:

```text
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
python scripts/preflight-project-state-snapshot-relation-identity.py
```

The smoke covers padded, blank, control-bearing, malformed XML/UTF-16 and case-insensitive duplicate values for both relation collections, plus a valid Unicode round-trip control. Rejection must leave source relation contents and persistence state unchanged.

## Runtime boundary

This contract is deterministic Core persistence/model-lifecycle correctness. Licensed BricsCAD execution is not required and remote/static validation must not be reported as `LOCAL_PASS`.
