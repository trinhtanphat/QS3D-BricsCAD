# Agent Work Claim — Project Browser semantic element ID canonicality

- Agent: `chatgpt-gpt56sol-browser-element-id-canonicality`
- Owner: OpenAI ChatGPT
- Status: `ACTIVE`
- Registered: 2026-08-12 08:46 +07:00
- Baseline main SHA observed: `6d18a2f86b714774750ce56b976ca2a3b2d43c7b`
- Task key: `CORE-BROWSER-ELEMENT-ID-CANONICALITY`

## Confirmed defect

`ProjectBrowserPlanner.ValidateAndOrderElements()` rejects blank and duplicate semantic element IDs, but uses `element.Id.Trim()` only for duplicate detection and then preserves the raw padded ID in browser node output. The same planner already rejects surrounding whitespace in FloorId/ZoneId references, and persisted semantic IDs are canonical structural identities. A padded element ID can therefore be silently normalized for uniqueness while leaking a different raw identity into browser navigation/paging.

## Reserved scope

- `src/QS3D.Core/Navigation/ProjectBrowserPlanner.cs`
- one focused Core smoke source for Project Browser element-ID canonicality
- this claim file

## Excluded scope

- Project Browser workspace persistence/query/selection/virtualization changes
- Floor/Zone/Family active-ID semantics
- BricsCAD adapters/UI/runtime
- Actions/build/release

## Plan

1. Re-fetch moving `main` and confirm the planner still accepts a padded semantic element ID.
2. Fail closed when a non-empty semantic element ID has surrounding whitespace, before browser node construction.
3. Preserve case-insensitive duplicate detection, valid canonical IDs, ordering, grouping and reference validation.
4. Add focused deterministic Core smoke coverage for padded-ID rejection plus a canonical valid case.
5. Merge only after exact moving-main diff/collision review, then close this claim with immutable evidence.

No GitHub Actions/build/release is authorized by this lane. No BricsCAD V25/V26 runtime PASS will be claimed remotely.
