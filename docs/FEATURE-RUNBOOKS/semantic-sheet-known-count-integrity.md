# Semantic Sheet known-Count traversal integrity

Lane-Key: `issue-4537`

## Boundary

`SemanticSheetDefinition` and `SemanticSheetPlanner` consume caller-controlled enumerable data at three documentation/model-planning boundaries: sheet placements, catalog sheet definitions, and available semantic views. Supported `ICollection<T>`, `IReadOnlyCollection<T>`, and non-generic `ICollection` Count surfaces are integrity evidence, not merely allocation hints.

For each boundary, a successful `MoveNext` is admitted against the established hard ceiling and the captured known Count before `IEnumerator.Current` is observed. A source advertising Count=N that yields N+1 therefore fails on the first unexpected successful `MoveNext` without exposing N+1 `Current`. Under-yield also fails closed.

After exact traversal, every supported Count surface is read again through the same admission helper. A changed, negative, oversized, or newly conflicting Count fails before planner output can be returned. Pure streaming inputs with no supported Count surface remain supported and retain the existing 128-placement / 10,000-catalog / 10,000-view ceilings.

## Deterministic regression

Run:

```text
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
python scripts/preflight-semantic-sheet-known-count-integrity.py
```

`SemanticSheetKnownCountIntegritySmoke` independently tracks Count reads, `MoveNext` calls, and `Current` reads for all three boundaries. The N+1 cases require two successful/attempted traversal steps with only one admitted `Current`, while drift cases require a second Count read after exact traversal. Existing Semantic Sheet known-Count contract smoke continues to cover malformed admission, under-yield, stable multi-interface evidence, honest counted inputs, and streaming behavior.

This is deterministic Core documentation/model-planning validation. It does not execute licensed BricsCAD and does not establish or claim `LOCAL_PASS`.
