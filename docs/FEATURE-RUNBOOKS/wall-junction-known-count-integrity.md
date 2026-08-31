# Wall junction known-Count integrity

## Scope

Core-only regression contract for `WallJunctionPlanner.Plan(IEnumerable<WallAxisSegment>)`. Licensed BricsCAD execution is not required for this boundary.

## Acceptance

The planner must preserve single-pass pure-streaming support and the historical 10,000-segment hard cap while treating any supported caller-known Count surface as integrity evidence.

For `ICollection<WallAxisSegment>`, `IReadOnlyCollection<WallAxisSegment>`, and non-generic `ICollection` inputs:

- reject negative, conflicting, or admission-oversized Count values;
- revalidate the admitted Count before and after caller-controlled `MoveNext`, immediately after `Current`, and after traversal;
- reject declared-count overrun before reading an unexpected `Current`;
- reject under-yield/final drift;
- retain a segment only after post-`Current` Count validation.

## Deterministic verification

Run:

```text
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
python scripts/preflight-wall-junction-known-count-integrity.py
```

`WallJunctionKnownCountIntegritySmoke` covers Count=0 over-yield/no-Current, transient MoveNext drift, transient Current drift, stable counted input, and pure-streaming input. `WallJunctionEnumerationCapSmoke` remains the streaming 10,001st-yield sentinel contract.

No hosted/source result may be described as licensed BricsCAD `LOCAL_PASS`.
