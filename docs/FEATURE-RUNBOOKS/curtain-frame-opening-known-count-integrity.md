# Curtain frame/opening known-Count integrity

Lane-Key: `issue-4517`

## Purpose

`CurtainFrameOpeningPlanner.Interrupt` accepts caller-controlled frame and opening enumerables and publishes deterministic geometry after bounded materialization and subtraction. Existing Count admission and observed-cardinality checks did not prevent an item beyond an admitted Count from exposing `IEnumerator.Current`, and they did not prove Count metadata stayed stable through exact traversal.

## Contract

For both frame and opening inputs:

1. Snapshot all supported `ICollection<T>`, `IReadOnlyCollection<T>`, and non-generic `ICollection` Count surfaces before enumeration.
2. Preserve initial negative/conflicting/oversized Count rejection.
3. Traverse as `MoveNext -> admitted Count guard -> independent safety guard -> Current -> validation/materialization` so N+1 Current is never observed.
4. Preserve exact under-yield rejection.
5. Rebind all supported Count surfaces after exact traversal and reject negative/conflicting/changed/source-set drift before subtraction or publication.
6. Preserve pure-streaming semantics, frame/opening bounds, null and finite validation, deterministic subtraction order and fragment ceiling.

## Deterministic validation

`CurtainFrameOpeningKnownCountIntegritySmoke` covers frame and opening no-overread, under-yield, post-traversal Count drift/negative/conflict, stable multi-interface counted sources and pure streaming controls.

`preflight-curtain-frame-opening-known-count-integrity.py` is auto-discovered and pins the traversal ordering, post-traversal rebind, self-registering smoke and this runbook.

## Runtime boundary

This is deterministic Core geometry correctness. Runtime is `NOT_APPLICABLE`; no licensed BricsCAD/private-DWG `LOCAL_PASS` is required or claimed.

## Landing boundary

Require exact-head Shared branch CI, latest-main non-force reconciliation when necessary, protected current-candidate `preflight` + `core` SUCCESS, expected-head merge and exact protected-main parent/source verification.
