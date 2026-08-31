# Rebar schedule known-Count integrity

Lane-Key: `issue-4508`

## Contract

`RebarScheduleBuilder.Build` accepts both deterministic counted collections and pure streaming `IEnumerable<RebarScheduleInput>` sources. For sources exposing `ICollection<T>.Count`, `IReadOnlyCollection<T>.Count`, or non-generic `ICollection.Count`, all available Count surfaces are admission evidence and must agree before traversal.

Caller-controlled traversal is ordered strictly as:

`MoveNext -> admitted Count overrun guard -> independent 10,000-row guard -> Current -> semantic validation/append`.

A Count=N source must therefore fail on the N+1 `MoveNext` without evaluating `IEnumerator.Current` for item N+1. Pure streaming sources retain the independent row boundary and likewise must not read `Current` after 10,000 rows have already been accepted.

After traversal, deterministic Count evidence is read again through the same validator. Negative, conflicting, oversized, or changed evidence fails closed before aggregate validation/result publication. Exact under-yield remains rejected. Stable counted collections and pure streaming inputs remain accepted.

## Deterministic coverage

`RebarScheduleKnownCountIntegritySmoke` is module-initialized and covers:

- negative/conflicting/oversized Count rejection before enumeration;
- exact under-yield rejection;
- Count overrun rejection before N+1 `Current`;
- post-traversal Count drift, negative Count, and cross-interface conflict;
- stable multi-interface Count evidence;
- exact 10,000 counted input;
- pure streaming input;
- streaming row-bound rejection before `Current` 10,001.

## Validation

Run:

```text
python scripts/preflight-rebar-schedule-known-count-integrity.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

Shared branch CI is required on the exact canonical branch head before opening the PR. Merge requires fresh protected current-candidate `preflight` and `core` SUCCESS. Licensed BricsCAD runtime is not applicable to this deterministic Core contract.
