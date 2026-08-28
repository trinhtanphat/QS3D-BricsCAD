# MEP/TBQ report traversal bound

Status: `SOURCE_FIX_ACTIVE / REMOTE_SAFE`

- Lane-Key: `issue-4383`
- Canonical owner/session: `account-local QS3D schedule worker C01 / 2026-08-28`
- Canonical branch: `agent/c01/issue-4383-mep-tbq-count-bound`
- Baseline: `main@f6a68f8618999305c46da1cc49b925576ccdd5e6`
- Runtime: `NOT_APPLICABLE` — deterministic Core MEP/TBQ integrity only

## Problem

`MepTbqProjectionService.BuildReport(IEnumerable<MepQuantityGroup>)` previously materialized a caller-controlled enumerable without a finite traversal bound or a deterministic collection `Count` contract. A broken or hostile source could therefore keep yielding valid groups and grow the report list before sorting/publication. Sources exposing collection Count metadata could also over-yield, under-yield, or change Count during traversal without rejection.

This lane is intentionally separate from `issue-4377`, which owns `MepQuantityService.Aggregate` / `MepQuantity.cs`. The projection lane consumes already-produced quantity groups and does not change MEP quantity aggregation semantics.

## Source contract

`MepTbqProjectionService.BuildReport` now enforces these boundaries before report publication:

1. Inspect deterministic Count evidence from `ICollection<MepQuantityGroup>`, `IReadOnlyCollection<MepQuantityGroup>`, and non-generic `ICollection` when present.
2. Reject negative or conflicting deterministic Count evidence before traversal.
3. Reject a deterministic Count above `MaxGroups = 10000` before requesting an enumerator.
4. For an admitted deterministic Count, reject the first item beyond that Count before null validation or `MepTbqReportRow` materialization.
5. Preserve the independent 10,000-group cap for pure streaming sources with no deterministic Count surface.
6. Reject deterministic Count under-yield after traversal.
7. Rebind deterministic Count evidence after exact traversal and reject Count disappearance, drift, negative evidence, conflict, or oversized replacement before sort/publication.
8. Only after all cardinality/stability checks pass may rows be sorted and published.

## Compatibility invariants

Valid inputs retain existing behavior:

- null-group validation remains fail-closed;
- `double` to TBQ `decimal` representability checks are unchanged;
- deterministic Region/System/Specification/Kind sorting remains unchanged;
- CSV schema/escaping and stable item hashing remain unchanged;
- projected bill-row metric semantics and preservation of non-MEP workspace state remain unchanged;
- pure streaming callers remain supported through exactly 10,000 groups.

## Deterministic regression coverage

`MepTbqProjectionSmoke` covers:

- known Count overrun winning before validation/materialization of the extra row;
- known Count under-yield;
- Count drift after traversal;
- oversized known Count rejection before enumeration starts;
- pure streaming traversal bounded at the first item beyond 10,000;
- all pre-existing projection, CSV, Unicode, zero-metric, and decimal-range cases.

`scripts/preflight-mep-tbq-count-bound.py` locks the production validation order and regression registration tokens so future source changes cannot silently remove the cardinality contract.

## Validation / landing

Required repository-safe evidence:

1. focused feature preflight PASS;
2. deterministic Core smoke/build PASS on the exact branch head;
3. automatic exact-head branch Shared CI SUCCESS before opening the canonical PR;
4. refresh/reconcile current `main` non-force if it moves;
5. protected current-candidate `preflight` + `core` SUCCESS;
6. merge the same canonical PR only when current and mergeable, then verify exact protected `main` contains the change.

No licensed BricsCAD, private-DWG, signing, package, or `LOCAL_PASS` evidence is required or claimed for this Core-only lane.
