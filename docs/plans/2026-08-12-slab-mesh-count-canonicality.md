# Slab Mesh generated-count canonicality plan

## Goal

Make Slab Mesh health fail-visible when persisted `GeneratedSlabMeshCount` is numerically valid but not in the exact invariant integer form emitted by the production writer.

## Evidence

`SlabMeshSolidBuilder.CommitSemanticUpdate(...)` writes `GeneratedSlabMeshCount` with `update.Handles.Count.ToString(CultureInfo.InvariantCulture)`. `GeneratedSlabMeshHealthService.Inspect(...)` currently accepts broader `NumberStyles.Integer` spellings whenever their numeric value matches the number of valid handles.

## Implementation

- Preserve the existing missing/invalid/negative/mismatch branch.
- After a successful matching parse, compare stored text ordinally with `count.ToString(CultureInfo.InvariantCulture)`.
- Emit `SLAB_MESH_GENERATED_COUNT_NON_CANONICAL` at warning severity for alias spellings.
- Do not normalize or rewrite project metadata from health inspection.

## Regression

Focused auto-registered Core smoke:

- canonical `2` remains healthy;
- `+2`, `02`, and padded ` 2 ` each surface the new issue without a mismatch issue;
- canonical `1` with two handles preserves `SLAB_MESH_GENERATED_COUNT_MISMATCH`.

## Validation

Verify exact source/test diffs and ancestry on moving `main`. Do not dispatch GitHub Actions or claim executable/.NET/BricsCAD runtime PASS without execution.
