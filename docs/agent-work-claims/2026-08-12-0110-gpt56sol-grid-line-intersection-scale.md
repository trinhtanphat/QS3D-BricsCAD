# Work claim — Grid LINE intersection scale

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-grid-line-intersection-scale-20260812-0110`
- Registered: `2026-08-12T01:10:00+07:00`
- Baseline main SHA: `388de3818354b7e0849fc82bca896ea92cb7b49b`
- Priority: evidence-driven Core numeric hardening during owner-requested `continue all`

## Reserved scope

Make `GridIntersectionPlanner` LINE/LINE determinant and cross-tolerance evaluation accept representable finite results even when raw component or length-product intermediates overflow.

## Concrete defect

For two finite LINE references around `1e160`, the true cross product can remain finite through cancellation and an explicitly small tolerance can make `tolerance * |r| * |s|` finite. The old code first evaluated raw component products in `Cross` and materialized `rLength * sLength`; either intermediate could overflow near `1e320` before cancellation or tolerance scaling, causing a false `OverflowException` even though the requested intersection calculation and result remained representable.

## Implementation

- `389c3e7771edbf21bec99130731e5e8f33b54109` — make the determinant helper scale-safe and introduce overflow-aware LINE/LINE cross-tolerance evaluation.
- `1aabeed6103ad8ce641cdb0ddf16247bc3e9b424` — self-audit refinement: preserve the exact prior tolerance calculation whenever `|r|*|s|` is finite, and only reorder multiplication when that length product itself overflows; this avoids introducing a tiny-tolerance underflow regression.
- `45b4cc270876a15e01673feb31924d1de82099ad` — add public coverage for two `1e160` near-parallel LINEs crossing near their midpoint with explicit `1e-15` tolerance, where raw component/length products overflow but determinant, cross tolerance and intersection remain finite.

## Concurrency handling

- The first regression-file creation attempt received HTTP 409 while `main` advanced through unrelated work.
- Re-fetched current `main` and target source, confirmed the source fix remained present, then retried the isolated new test file without force.

## Validation performed

- Re-fetched committed source and confirmed `Cross` uses scale-normalized determinant reconstruction.
- Re-fetched the final `LineCrossTolerance`: old arithmetic is retained for finite length products; overflow-safe ordering is used only when `rLength * sLength` is non-finite, and final non-finite tolerance still throws.
- Re-fetched the public smoke fixture and confirmed it expects exactly one finite midpoint intersection and preserves input pair identity.
- Source/static validation only; no GitHub Actions dispatched and no BricsCAD V25 runtime/build/NETLOAD PASS claimed.

## Explicit exclusions retained

- No LINE/ARC or ARC/ARC quadratic/circle math, ambiguity policy, default tolerance, curve validation/cardinality, identity/ownership, native V25 inspection, UI, Actions, release, or LOCAL_PASS behavior changes.

## Completion

Representable LINE/LINE intersections no longer fail solely on avoidable intermediate scale overflow, while truly non-finite determinant/tolerance results remain fail-closed, focused regression is integrated on `main`, and this claim is closed.
