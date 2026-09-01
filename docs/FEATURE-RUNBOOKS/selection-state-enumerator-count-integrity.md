# SelectionState enumerator-acquisition Count integrity

Issue: #5207
Lane-Key: `issue-5207`
Runtime: deterministic Core only; no licensed BricsCAD evidence is required.

## Defect

`SelectionState.Replace` already validates generic, read-only generic, and non-generic known Count before traversal and around `MoveNext`/`Current`. The caller-controlled `GetEnumerator()` call was previously outside that rebound window. A hostile counted enumerable could transiently alter Count during enumerator acquisition and restore it before the first loop guard, allowing the acquisition-time contradiction to escape detection.

## Contract

The same captured known Count is now rebound immediately before and immediately after `GetEnumerator()`. Any acquisition-time growth, shrink, negative Count, or conflict between Count surfaces fails before the first `MoveNext`, before `Current`, and before selection publication or `Changed` notification. Existing hard-cap, over/under-yield, mid-traversal Count stability, final Count/reentrancy, case-insensitive dedupe, whitespace normalization, and pure-streaming behavior remain unchanged.

## Deterministic regression

`SelectionStateKnownCountStabilitySmoke` uses a hostile multi-interface counted enumerable whose Count changes only when `GetEnumerator()` is acquired. It verifies growth, shrink, negative and conflicting Count failures with exactly one acquisition and zero `MoveNext`/`Current` observations, while retaining stable counted and streaming controls.

Run from repository root:

```text
python scripts/preflight-selection-state-enumerator-count-integrity.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
dotnet build src/QS3D.Core/QS3D.Core.csproj -c Release
```
