# MeasurementTrace known-Count traversal stability

## Scope

This contract applies to `MeasurementTraceContract` snapshotting of input facts, adjustments, warnings, and assumptions. It is deterministic Core behavior and does not require licensed BricsCAD execution.

## Integrity boundary

When a caller-owned enumerable exposes a supported Count through `ICollection<T>`, `IReadOnlyCollection<T>`, or non-generic `ICollection`, that Count is admission evidence. Enumerator acquisition, `MoveNext()` and `Current` are caller-controlled operations, so the admitted Count must still match immediately after those boundaries before traversal output can be accepted.

For each measurement trace collection the implementation therefore:

1. admits and cross-checks supported Count contracts and the 10,000-entry ceiling;
2. acquires the enumerator and immediately revalidates the admitted Count before any `MoveNext()`;
3. revalidates immediately before and after each `MoveNext()`;
4. when an item exists, enforces the admitted traversal capacity before reading `Current`;
5. revalidates Count immediately after `Current` and before validating or retaining that item;
6. verifies observed item count and final known Count before canonical sorting/publication.

A source without a supported Count remains a streaming source and retains the existing 10,000-entry traversal ceiling. The contract detects Count drift that is observable at the defined boundaries; it does not claim to detect a mutation that is both introduced and completely restored inside one opaque caller callback.

## Deterministic regression

`MeasurementTraceKnownCountStabilitySmoke` covers persistent hostile Count drift at enumerator acquisition, `MoveNext`, and `Current`; it proves acquisition drift receives zero `MoveNext`/`Current` calls, MoveNext drift is rejected before `Current`, and Current drift is rejected before further traversal. Adjustment and message surfaces are independently checked, while stable counted and unknown-count streaming controls must still succeed.

`scripts/preflight-measurement-trace-known-count-stability.py` pins the production ordering on facts, adjustments, and messages, pins canonical smoke registration, and contains a negative source mutation proving removal of the post-acquisition rebound fails the guard.

## Validation

Run:

```text
python scripts/preflight-measurement-trace-known-count-stability.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

Protected merge qualification still requires fresh exact-candidate Shared CI `preflight` and `core` success, current-main collision/freshness reconciliation, and normal expected-head PR merge. Runtime classification is `NOT_APPLICABLE`; hosted/static evidence must not be described as licensed BricsCAD `LOCAL_PASS`.
