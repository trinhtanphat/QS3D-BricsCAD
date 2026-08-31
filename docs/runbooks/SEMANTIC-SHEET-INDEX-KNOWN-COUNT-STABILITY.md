# Semantic sheet index known-Count stability

Carrier: `issue-4458`
Reservation protocol: `v2`
Canonical branch: `agent/longnguyentuan2107-maker-c01-20260829-02/issue-4458-sheet-index-count-stability`

## Defect

`SemanticSheetIndexBuilder.MaterializeBounded` admitted known collection `Count` evidence before traversal but previously validated it only after traversal. A Count=N source yielding N+1 sheets could therefore expose and retain N+1 `Current` before the mismatch was rejected. The materializer also did not re-read Count surfaces after traversal, so mutable Count evidence could drift while the index snapshot was being consumed.

## Required invariant

Materialization must be ordered as follows:

1. validate all available known Count surfaces before traversal, including negative, conflicting and 10,000-sheet ceiling checks;
2. call `MoveNext()`;
3. reject known-Count overrun before reading `Current`;
4. reject the pure-streaming 10,000-sheet ceiling before reading `Current`;
5. read `Current`, reject null sheets, and retain the sheet;
6. after exact traversal, reject known-Count under-yield;
7. re-read all Count surfaces and reject any negative, conflicting, oversized or changed Count evidence;
8. only then proceed to duplicate-id/number validation and deterministic index sorting.

## Deterministic validation

Run from repository root:

```text
python scripts/preflight-semantic-sheet-index-known-count-stability.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
dotnet build src/QS3D.Core/QS3D.Core.csproj -c Release
```

The adversarial smoke instruments `MoveNext`, `Current`, Count reads and enumerator admission. A Count=1 source that yields a second sheet must reach the second `MoveNext` while still reporting exactly one `Current` read. The smoke also covers under-yield, post-traversal Count drift, conflicting/negative Count surfaces, the 10,000-sheet streaming ceiling, null entries, honest deterministic sorting, and duplicate-number rejection.

This is a Core-only deterministic acceptance package. No licensed BricsCAD runtime PASS is required or claimed.
