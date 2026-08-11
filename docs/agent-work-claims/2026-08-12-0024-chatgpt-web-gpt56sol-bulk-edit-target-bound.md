# Agent work claim — Bulk edit target enumeration bound

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-12T00:24:00+07:00
- Status: `ACTIVE`
- Baseline main SHA: `f19d3786d444a4a86cfb7e53a7f7ec4405804629`
- Scope: bound raw target enumeration accepted by `BulkEditService` so arbitrary/lazy `IEnumerable<ProjectElement>` or ID input cannot enumerate indefinitely before an all-or-nothing bulk mutation decision.
- Evidence: `OwnedDistinct(...)` currently enumerates target objects into a dictionary with no raw-input ceiling; an infinite enumerable repeating the same valid project-owned element never terminates because duplicates overwrite the same dictionary entry. `OwnedDistinctByIds(...)` also has no explicit public-API envelope. Live semantic selection/workspace already uses a 10,000-entry bound.
- Files reserved:
  - `src/QS3D.Core/Services/BulkEditService.cs`
  - `tests/QS3D.Core.SmokeTests/BulkEditTargetInputBoundSmoke.cs`
  - this claim file for close-out
- Plan:
  1. Enforce a 10,000 raw-entry ceiling for both object-target and ID-target enumeration, with known-count fast-fail and lazy max+1 termination.
  2. Preserve existing semantics inside the bound: object overload exact project ownership + case-insensitive dedupe; ID overload blank rejection + duplicate-ID rejection + canonical lookup.
  3. Complete validation/materialization before mutation so oversize input cannot partially edit elements, touch project revision, or emit a changed result.
  4. Add CAD-independent smoke coverage for exact boundary, infinite/repeating lazy object input, known/lazy ID oversize input and read-only failure.
  5. Refresh current `main`, verify reachability/current source, then release the claim.
- Non-overlap: no WPF/native bulk-edit UI, no active Family mutation implementation lane, no selection bridge/PICKFIRST, no persistence schema and no BricsCAD runtime work.
- Validation boundary: source/smoke contract review only; no GitHub Actions dispatch and no licensed BricsCAD V25/V26 runtime PASS.
