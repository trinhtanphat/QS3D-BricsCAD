# Project persistence known-Count Current no-overread

Issue: #4481  
Lane-Key: `issue-4481`  
Runtime: `NOT_APPLICABLE` — deterministic Core persistence/data-integrity contract.

## Boundary

`ProjectPersistenceStamp` materializes a stable semantic snapshot used by save/dirty-state decisions. Its shared `SnapshotBounded<T>` helper is used by project metadata, zones, floors, families and nested properties, quantity rules, elements and nested source/dependency/property/quantity collections, and audit events.

A collection with deterministic `Count` is caller-controlled evidence. C# `foreach` reads `IEnumerator.Current` immediately after a successful `MoveNext()`, before loop-body Count and 10,000-entry guards execute. The persistence boundary must therefore use an explicit enumerator.

## Required ordering

For each successful `MoveNext()`:

1. reject item 10,001 at the independent hard ceiling;
2. reject the first item beyond the admitted known Count;
3. only then read `Current`;
4. only then retain the value.

After traversal, exact cardinality must match the admitted Count and every supported deterministic Count surface (`ICollection<T>`, `IReadOnlyCollection<T>`, non-generic `ICollection`) must still agree with that Count before the materialized list is returned.

## Fail-closed cases

The deterministic smoke requires:

- Count=1 / yield=2: second `MoveNext()` is observed but second `Current` is never read;
- Count=10,000 / yield=10,001: item 10,001 is detected by `MoveNext()` and rejected by the hard ceiling before `Current` 10,001;
- exact two-item traversal whose Count changes after terminal `MoveNext=false`: reject before snapshot publication;
- conflicting supported Count interfaces: reject before enumeration;
- stable counted input: preserve exact values/order and normal terminal enumeration.

Under-yield, negative/oversized Count, stable project-boundary validation, nested deterministic encoding, semantic metadata filtering and atomic `MarkSaved` publication remain authoritative existing behavior.

## Validation

Run the repository aggregate feature preflights and `QS3D.Core.SmokeTests`. The dedicated auto-discovered guard is `scripts/preflight-project-persistence-known-count-no-overread.py`; the adversarial self-registering smoke is `ProjectPersistenceStampKnownCountNoOverreadSmoke`.

Hosted/source validation is sufficient for this Core-only contract. Do not claim licensed BricsCAD `LOCAL_PASS` from this runbook.
