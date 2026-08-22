# Agent work claim — Physical opening cut target owner binding

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-11 (UTC+7)
- Status: `DONE`
- Scope: fail closed when physical-opening persisted target-state is resolved against a detached/foreign host object instead of the exact canonical `ProjectElement` owned by the current `ProjectState`.
- Evidence: `PhysicalOpeningCutTargetStateCodec.Resolve(ProjectState, ProjectElement, IEnumerable<string>)` previously validated opening IDs/category/HostWallId but did not prove that its `host` argument was the exact project-owned instance. A detached host with the same ID could therefore participate in target-state validation even though project mutation/ownership services otherwise require exact object ownership.
- Files reserved during work:
  - `src/QS3D.Core/Services/PhysicalOpeningCutTargetStateCodec.cs`
  - `tests/QS3D.Core.SmokeTests/PhysicalOpeningCutTargetStateOwnerBindingSmoke.cs`
  - this claim file for close-out
- Implemented:
  1. `Resolve(...)` now resolves the canonical host through `project.FindElement(host.Id)` before target normalization/resolution.
  2. A missing project host or detached same-ID host is rejected; duplicate/corrupt project IDs continue to fail closed through `ProjectState.FindElement`.
  3. Valid resolution uses the canonical host identity for `HostWallId` relation checks and preserves existing target ordering/category rules.
  4. Added CAD-independent smoke coverage proving canonical success is read-only plus detached same-ID and absent-host rejection without `ChangeVersion` mutation.
- Implementation commit: `07f0ac30c39572d1bcc411ea41668e3f84fe58bb` (`fix(opening): bind cut target resolution to project host`).
- Regression commit: `abeb8e649bbf309e14c0811b7cd0e263682ffaa8` (`test(opening): guard cut target host ownership`).
- Integration verification: both commits were confirmed reachable from later `main` (`abeb8e6...` was an ancestor of `870afd5...`); current source still contains the exact-reference guard after concurrent unrelated commits.
- Validation boundary: source/test contract reviewed remotely. GitHub Actions were not dispatched; the smoke executable was not claimed as run in this session; no licensed BricsCAD V25 runtime PASS is claimed.
- Non-overlap: native opening Boolean execution, Source Reconcile, generated rebar, Build3D, Curtain, Grid, documentation tables/sheets and V25/runtime lanes were not modified.
- Reservation released: this claim is complete and no longer reserves the listed files.
