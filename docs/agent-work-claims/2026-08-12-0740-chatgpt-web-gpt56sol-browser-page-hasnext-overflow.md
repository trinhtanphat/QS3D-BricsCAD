# Work claim — Project Browser page HasNext overflow

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-browser-page-hasnext-overflow-20260812-0740`
- Registered: `2026-08-12T07:40:00+07:00`
- Baseline main SHA: `bce535e8d684bb95ec34266b3d321610973ae513`
- Priority: P2 — keep bounded Project Browser paging arithmetic correct at the full `int` domain.

## Reserved scope

`ProjectBrowserElementPage.HasNext` currently evaluates `Offset + ElementIds.Count < TotalCount` using `int` arithmetic. Unlike viewport rows, node element counts are not bounded by `MaxNodes`; a page near `int.MaxValue` can overflow the addition and report `HasNext=true` after the final page. The `MaxNodes = 500000` index boundary itself is correct and is not being changed.

## Reserved surfaces

- `src/QS3D.Core/Navigation/ProjectBrowserVirtualizationPlanner.cs`
- `tests/QS3D.Core.SmokeTests/ProjectBrowserPageArithmeticSmoke.cs` (new focused module-initializer regression)
- this claim file

## Intended fix

- Evaluate page end arithmetic in a widened integer domain so `HasNext` cannot wrap.
- Keep `MaxNodes`, paging limits, index traversal, element enumeration, path semantics and viewport behavior unchanged.
- Add a focused regression that constructs the public page result at an extreme logical boundary without allocating an extreme number of element IDs and verifies final/non-final `HasNext` behavior.

## Validation boundary

Committed deterministic Core smoke coverage plus exact source/diff review. No GitHub Actions dispatch; no BricsCAD V25 runtime PASS claimed.

## Completion condition

The overflow is eliminated on current `main`, `MaxNodes` remains unchanged, and this claim records exact integration evidence without overlapping another ACTIVE browser lane.
