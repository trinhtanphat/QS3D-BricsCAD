# Work claim — Project Browser query option bounded enumeration

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T23:50:00+07:00`
- Baseline main SHA: `4ec0e38a9bc0a331302a7fde6966da86d2773d9f`
- Priority: evidence-driven remote-safe Core regression hardening

## Confirmed defect

`ProjectBrowserQueryPlanner` defines a 10,000-ID filter bound, but `ProjectBrowserQueryOptions` eagerly materializes caller-provided `IEnumerable<ElementCategory>`, `IEnumerable<string>` floor IDs and zone IDs into unbounded `List<T>` instances before the planner can enforce that bound. An infinite or excessively large enumerable can therefore hang or exhaust memory before the existing fail-closed filter-size validation is reached.

## Reserved scope

Bound query-option enumerable materialization at construction time while preserving normal query/filter semantics, existing 10,000 floor/zone filter capacity, category validation, ordering, duplicate checks and browser result behavior.

## Expected surfaces

- `src/QS3D.Core/Navigation/ProjectBrowserQueryPlanner.cs`
- `tests/QS3D.Core.SmokeTests/ProjectBrowserQueryOptionBoundsSmoke.cs`
- `tests/QS3D.Core.SmokeTests/ProjectBrowserQueryOptionBoundsRegistration.cs`
- this claim file

## Excluded scope

- No Project Browser selection, virtualization, workspace-state/UI, grouping semantics or native BricsCAD changes.
- No changes to project/domain identity rules.
- No GitHub Actions dispatch.

## Validation plan

- Preserve null/empty option collections.
- Preserve a normal finite category/floor/zone option set.
- Prove floor/zone option enumeration fails immediately after the documented 10,000-item cap instead of consuming an unbounded source.
- Bound category option enumeration as defensive input hardening without changing defined-category validation in the planner.
- Use a dedicated module initializer to avoid shared smoke registration contention.
- Re-fetch target blob immediately before the product write and review exact pushed diffs/ancestry.
- Hosted environment has no .NET SDK; no executed `dotnet` or V25 runtime PASS will be claimed.

## Coordination

Recent browser claims target selection case identity, viewport windowing/node caps and workspace XML. This lane is confined to `ProjectBrowserQueryOptions` enumeration bounds in the query planner file and dedicated smoke files.

## Completion condition

Query-option enumeration is bounded before materialization, normal filter behavior is preserved, focused regression source is registered on current `main`, concurrent work is preserved, and this claim is closed with exact commit SHAs.