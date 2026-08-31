# Curtain Wall aggregation precision

Lane-Key: `issue-4649`

## Scope

`CurtainWallScheduleBuilder` groups GlassWall elements by normalized Floor and Family. Its six continuous commercial metrics must retain small finite contributions across high dynamic range without weakening the existing fail-closed requirement for a final value that binary64 cannot represent.

## Invariant

For each group and each of `TotalWallLengthM`, `GrossWallAreaM2`, `OpeningAreaM2`, `NetGlassAreaM2`, `FrameFaceAreaM2`, and `FrameLengthM`:

1. validate every incoming quantity as finite and non-negative;
2. accumulate into isolated compensated state instead of publishing pairwise row totals;
3. reject sum/compensation overflow immediately;
4. finalize only after traversal;
5. fail closed if a material non-zero compensation cannot be represented in the final binary64 result.

The final representability check deliberately preserves historical strictness: `2^53 + 1` must not silently publish as `2^53`, while `1e16 + 1 + 1` must publish the representable `10000000000000002`.

Integer counts, grouping/order, min/max clear-panel dimensions, Family/category checks and row provenance remain independent and unchanged.

## Deterministic regression

`CurtainWallAggregationPrecisionSmoke` covers large-first and small-first high-dynamic-range inputs, metric/group isolation, final-unrepresentable refusal and non-finite refusal. The smoke self-registers through a module initializer.

## Validation

Run:

```text
python scripts/preflight-curtain-wall-aggregation-precision.py
python scripts/preflight-all.py
dotnet build src/QS3D.Core/QS3D.Core.csproj -c Release
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

Repository Shared CI is authoritative for the exact pushed candidate. This is deterministic Core reporting work; licensed BricsCAD/private-DWG runtime evidence is `NOT_APPLICABLE` and no `LOCAL_PASS` is produced.
