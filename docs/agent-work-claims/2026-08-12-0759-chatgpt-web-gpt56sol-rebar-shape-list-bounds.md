# Work claim — Rebar shape list finite bounds

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-rebar-shape-list-bounds-20260812-0759`
- Registered: `2026-08-12T07:59:00+07:00`
- Baseline main SHA: `c5b51651bed94fbb54268b506c338c8547f4c966`
- Priority: evidence-driven remote-safe resource bounds during owner-requested `continue all`

## Confirmed defect

`RebarShapePathBuilder` advertises `MaxLegs = 32`, but `ParsePositiveList()` and `ParseTurns()` call `string.Split(...)` and materialize every token before the leg-count contract is enforced. Persisted `RebarShapeLegsM` / `RebarShapeTurnsDeg` strings therefore have no pre-allocation bound and can consume memory/CPU far beyond the supported 32-leg shape before eventually failing.

## Reserved scope

- `src/QS3D.Core/Rebar/RebarShapePath.cs` list parsing/resource bounds only.
- `tests/QS3D.Core.SmokeTests/RebarShapePathBoundsSmoke.cs` focused CAD-independent regression.
- this claim file.

## Contract

- Reject legs/turns text longer than 4096 UTF-16 characters before splitting.
- Reject more than 32 leg tokens during parsing rather than after fully materializing an oversized list.
- Reject more turn tokens than the current shape can legally consume (`legs - 1`) during parsing.
- Preserve accepted shape codes, invariant numeric grammar, finite/positive leg semantics, ±180° turn policy, exact `legs-1` cardinality, cutting-length validation and geometry output.
- Boundary-valid 32-leg custom shapes remain supported.

## Excluded scope

No CAD/native placement, Level Z-chain, shape distribution, fabrication standards, generated ownership, BBS quantity semantics, release/update or UI changes. Do not edit the completed RebarShapePath aliasing contract.

## Validation plan

Add module-initializer smoke coverage for the 4096/4097 text boundary, 32/33 leg-token boundary, legal custom turn cardinality and ordinary L/U/straight behavior. Re-fetch current source before each write; never force-push. No GitHub Actions dispatch and no BricsCAD V25/V26 runtime qualification claim.
