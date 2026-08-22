# Work claim — Project Browser selection root-membership complexity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-browser-selection-root-membership-complexity-20260812-1042`
- Registered: `2026-08-12T10:42:00+07:00`
- Baseline main SHA: `d049ae89c17e037148d23c9f2dba1aec35609569`
- Priority: P2 — keep bounded multi-selection validation proportional to the documented selection/tree limits.

## Confirmed defect

`ProjectBrowserSelectionPlanner.BuildIndex(...)` stores each node membership set in `HashSet<string>(StringComparer.OrdinalIgnoreCase)`. `PlanReveal(...)` nevertheless validates each selected element with `index.Root.ElementIds.Contains(elementId, StringComparer.OrdinalIgnoreCase)`. Because the two-argument overload is LINQ `Enumerable.Contains`, it linearly enumerates the HashSet instead of using its O(1) instance lookup. At the supported bounds (10,000 selected IDs and up to 250,000 root elements from the Browser planner), root-membership validation can perform roughly 2.5 billion string comparisons before reveal planning even starts.

## Reserved surfaces

- `src/QS3D.Core/Navigation/ProjectBrowserSelectionPlanner.cs` — one root-membership lookup only
- `tests/QS3D.Core.SmokeTests/ProjectBrowserSelectionRootMembershipSmoke.cs` — new focused semantic-regression smoke
- this claim file

## Intended fix

- Use the existing case-insensitive HashSet instance `Contains(elementId)` for root membership.
- Preserve missing-element failure, canonical selected-ID normalization, sorting, primary selection, ambiguity detection, expansion/target paths and all node/index limits.
- Add focused smoke proving case-insensitive selected IDs still resolve correctly after the comparer-overload removal and missing IDs still fail closed.
- Exact source diff must be one-line semantic-equivalent complexity fix; no timing threshold is used in regression because wall-clock performance tests are nondeterministic.

## Coordination

Recent Browser query/reference/workspace lanes are completed or own different files. No UI/native/persistence files are in scope.

## Validation boundary

Committed deterministic semantic smoke coverage plus exact source/diff review. No GitHub Actions dispatch; no licensed BricsCAD V25/V26 runtime PASS claimed.

## Completion

- PR: `#798`
- Merge SHA: `3b3616b4633de79dd1e73bdda00edde67323c442`
- Product change: `ProjectBrowserSelectionPlanner` now uses the indexed case-insensitive `HashSet<string>.Contains` instance lookup for root membership.
- Regression: `ProjectBrowserSelectionRootMembershipSmoke` preserves case-insensitive selection semantics and fail-closed missing-ID behavior.
