# Semantic Schedule catalog save bounded-enumeration plan — 2026-08-12

## Goal

Make the existing 128-definition `SemanticScheduleCatalog.Save()` capacity bound source enumeration/allocation as well as accepted catalog cardinality, without changing semantic schedule persistence, schema, rendering or native placement behavior.

## Confirmed defect

Claim commit: `b3ea1c5720c03d9800d25e4feb4ebe20c02eaf5e`.

Post-claim source was re-fetched from moving `main` at `ec20e5b19af544262f0abc39432a225ad7231202` and still executed:

```text
var list = definitions.ToList();
ValidateCatalog(list);
```

`ValidateCatalog()` enforces `MaxSchedules = 128`, but only after the complete enumerable has already been consumed. A huge or non-terminating lazy source can therefore be enumerated and allocated without bound before the declared catalog capacity is reached.

## Invariants to preserve

- Public catalog capacity remains exactly 128 definitions.
- Accepted definitions still pass through the existing `ValidateCatalog()` and `Normalize()` rules.
- Duplicate id/name validation remains case-insensitive and unchanged.
- XML schema/version, canonical category names and metadata key remain unchanged.
- Empty catalog save still removes existing metadata and touches only when removal is needed.
- Identical serialized payload remains a true no-op.
- Real persistence changes still call `ProjectState.Touch()` exactly before metadata mutation.
- Payload size remains capped at 1 MiB.
- No Schedule Hub, native table, placement, Floor/Zone/Element reference or BricsCAD command behavior changes.

## Implementation

### 1. Bounded one-pass save materialization

Replace unrestricted `definitions.ToList()` with a single pass:

- buffer at most 128 definitions;
- when the source yields the 129th definition, throw the existing `InvalidOperationException` capacity message immediately;
- never request the 130th item after oversize cardinality is known;
- call the existing `ValidateCatalog(list)` only after successful bounded materialization.

This preserves validation semantics for every accepted input while making the existing capacity effective against lazy/unbounded sources.

### 2. Adversarial Core regression

`SemanticScheduleSaveBoundedEnumerationSmoke` uses an effectively infinite source of otherwise valid, unique schedule definitions. The source throws a sentinel exception if item 130 is requested.

Expected contract:

- `Save()` throws `Semantic schedule catalog exceeds the supported 128 definitions.`;
- exactly 129 source items are yielded;
- item 130 is never requested;
- `ProjectState.ChangeVersion` is unchanged;
- the semantic schedule metadata key is still absent.

The smoke is registered through a dedicated module initializer to avoid shared registration hotspots in this multi-agent repository.

### 3. Static preflight

`preflight-semantic-schedule-save-bounded-enumeration.py` requires:

- bounded list allocation and one-pass `foreach`;
- in-enumeration capacity guard before `list.Add`;
- existing exact capacity error;
- `ValidateCatalog(list)` after enumeration;
- all persistence `Touch()` calls after bounded enumeration/validation;
- adversarial 129/130 smoke evidence and module registration.

It rejects `definitions.ToList()` and post-materialization `list.Count > MaxSchedules` behavior inside `Save()`.

## Moving-main integration

- Work branch: `agent/semantic-schedule-save-bound-20260812`.
- Branch baseline: `ec20e5b19af544262f0abc39432a225ad7231202`.
- Refresh `main` before PR and again before merge.
- Compare changes since the branch baseline specifically for `SemanticScheduleCatalog.cs` and this lane's new files.
- If a concurrent winner changes the reserved source, do not overwrite it; re-read and reconcile only when scopes are demonstrably non-overlapping.
- Otherwise merge as a focused change and close the claim with exact PR/merge evidence.

## Validation policy

This lane is deterministic Core behavior. Source review plus committed smoke/static regression are the remote evidence available here. GitHub Actions are manual-only and are not dispatched. No licensed BricsCAD V25 runtime PASS is claimed without actual local evidence.
