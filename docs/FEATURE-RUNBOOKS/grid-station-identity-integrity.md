# Grid station identity integrity

Issue: #5203
Lane-Key: `issue-5203`
Runtime: deterministic Core only; no licensed BricsCAD evidence is required.

## Defect

The public `GridLinearStation`, `GridAngularStation`, and `GridRadialStation` constructors previously returned `value.Trim()` for semantic `ElementId`. This silently rewrote padded caller identity and allowed control characters or isolated UTF-16 surrogate code units to propagate into `GridReferenceCurve` ownership and downstream intersection/materialization identity. Planner `ValidateIds` also repeated trim-based aliasing before duplicate detection.

## Contract

All three public station types now share `GridStationIdentity.Normalize`. IDs must be nonblank, already free of surrounding whitespace, control-free, and valid Unicode scalar text. A valid high+low surrogate pair is preserved exactly, including case. `GridSystemPlanner.ValidateIds` rebounds the same contract before its existing case-insensitive uniqueness check, so planner output cannot depend on trim aliasing.

## Deterministic validation

Run from repository root:

```text
python scripts/preflight-grid-station-identity-integrity.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
dotnet build src/QS3D.Core/QS3D.Core.csproj -c Release
```

The smoke covers padded, control-character, isolated high/low surrogate identities, valid supplementary-plane Unicode preservation, case-insensitive duplicate IDs, rectangular/radial controls, and the existing precision-boundary scenarios.
