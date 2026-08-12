# Foundation Mesh generated-count canonicality plan

## Goal

Make Foundation Mesh health fail-visible when persisted `GeneratedFoundationMeshCount` is numerically valid but not in the exact invariant integer form emitted by the production builder.

## Evidence

`FoundationMeshSolidBuilder.CommitSemanticUpdate(...)` writes `GeneratedFoundationMeshCount` with `update.Handles.Count.ToString(CultureInfo.InvariantCulture)`. `GeneratedFoundationMeshHealthService.Inspect(...)` currently accepts broader `NumberStyles.Integer` spellings as long as the parsed value matches the number of valid handles.

## Implementation

- Keep the existing parse/range/count-mismatch branch unchanged.
- When parsing succeeds and the value is non-negative, compare the stored token byte-for-byte/ordinal with `count.ToString(CultureInfo.InvariantCulture)`.
- Emit a dedicated `FOUNDATION_MESH_GENERATED_COUNT_NON_CANONICAL` warning for an alias such as `+2`, `02`, or padded text.
- Do not normalize or rewrite persisted metadata during health inspection.

## Regression

Add a focused auto-registered Core smoke proving:

- canonical `2` with two valid handles does not produce the new issue;
- `+2`, `02`, and padded ` 2 ` each produce the new issue;
- existing numeric mismatch still produces `FOUNDATION_MESH_GENERATED_COUNT_MISMATCH`.

## Validation

Verify exact source/test diffs and ancestry against moving `main`. Do not dispatch GitHub Actions or claim executable/.NET/BricsCAD runtime PASS without execution.
