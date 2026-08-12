# Wall Mesh generated-count canonicality plan

## Goal

Make Wall Mesh health reject noncanonical aliases of the writer-owned generated count without changing numeric mismatch semantics.

## Evidence

The production writer stores `GeneratedWallMeshCount` using `update.Handles.Count.ToString(CultureInfo.InvariantCulture)`. Health currently parses with broad `NumberStyles.Integer`, allowing alternate spellings that the writer never produces.

## Implementation

Preserve the current missing/parse/range/count-mismatch branch. After a successful matching parse, compare the stored token ordinally with the invariant integer representation and emit `WALL_MESH_GENERATED_COUNT_NON_CANONICAL` when they differ. Keep inspection read-only.

## Regression

Cover canonical `2`, aliases `+2`/`02`/padded ` 2 `, and existing mismatch `1` against two valid handles.

## Validation

Verify exact diffs and ancestry only; no Actions/full build/executable smoke/BricsCAD runtime claims without execution.
