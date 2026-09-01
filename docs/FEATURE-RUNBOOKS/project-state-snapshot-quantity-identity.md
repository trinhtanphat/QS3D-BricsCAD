# Project State Snapshot Quantity Identity Integrity

## Scope

This Core-only contract protects mutable `ProjectElement.Quantities` identities when a `ProjectStateSnapshot` is captured, detached, or later used for rollback.

`ProjectElement.SetQuantity` is the canonical mutation API and trims quantity names before storing them, but `ProjectElement.Quantities` remains publicly mutable for compatibility. A caller can therefore bypass `SetQuantity` and inject a padded key such as `" NetVolumeM3 "`. Snapshot code must never silently normalize that hostile/transient identity while copying it.

## Invariant

Before any quantity is retained or republished by snapshot materialization:

- the existing 10,000-entry nested cardinality ceiling is enforced;
- the quantity name is nonblank and free of control characters;
- trimming the name must be a no-op (`Trim()` must equal the original value ordinally);
- the name must be valid XML text;
- quantity identities must remain unique under the canonical case-insensitive comparer;
- values must remain finite and nonnegative.

A non-canonical quantity name fails closed. Snapshot capture/detached-copy rejection must not mutate the source dictionary, element dirty/timestamp state, or project change/timestamp state.

Canonical XML-safe Unicode quantity identities are preserved exactly; snapshot code does not rename them. Quantity values retain their exact validated `double` semantics.

## Why this is required

Before this guard, `RequireCanonicalQuantities` derived `canonicalName = quantity.Key.Trim()` and validated the derived name but did not require equality with the original dictionary key. `CopyElementInto` later called `SetQuantity(quantity.Key, quantity.Value)`, which trims the key. As a result, a hostile padded dictionary identity could be accepted and silently renamed in detached/rollback state.

Snapshot is an identity-preserving transaction boundary, not a normalization boundary. Normalization belongs at canonical mutation APIs; unexpected mutable state must be rejected before rollback evidence is changed.

## Deterministic validation

Run:

```text
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
python scripts/preflight-project-state-snapshot-quantity-identity.py
```

The already-registered snapshot smoke covers padded quantity rejection through both `Capture` and `CreateDetachedCopy`, no source-state mutation on rejection, and exact Unicode quantity identity/value preservation. The focused preflight pins bound-before-validation, exact canonical-name equality, existing XML/value/collapse checks, and the existing smoke registration without editing the shared registry.

## Runtime boundary

This is deterministic Core persistence/model-lifecycle integrity. Licensed BricsCAD execution is not required and hosted validation must not be reported as `LOCAL_PASS`.
