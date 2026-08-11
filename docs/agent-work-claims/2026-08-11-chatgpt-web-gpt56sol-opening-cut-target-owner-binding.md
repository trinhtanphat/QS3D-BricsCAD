# Agent work claim — Physical opening cut target owner binding

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-11 (UTC+7)
- Status: `ACTIVE`
- Scope: fail closed when physical-opening persisted target-state is resolved against a detached/foreign host object instead of the exact canonical `ProjectElement` owned by the current `ProjectState`.
- Evidence: `PhysicalOpeningCutTargetStateCodec.Resolve(ProjectState, ProjectElement, IEnumerable<string>)` currently validates opening IDs/category/HostWallId but does not prove that its `host` argument is the exact project-owned instance. A detached host with the same ID can therefore participate in target-state validation even though project mutation/ownership services otherwise require exact object ownership.
- Files reserved:
  - `src/QS3D.Core/Services/PhysicalOpeningCutTargetStateCodec.cs`
  - `tests/QS3D.Core.SmokeTests/PhysicalOpeningCutTargetStateOwnerBindingSmoke.cs`
  - this claim file for close-out
- Implementation plan:
  1. Resolve the canonical host through `project.FindElement(host.Id)` and require reference identity with the supplied host before any target IDs are trusted.
  2. Reject a missing, duplicate/corrupt, or detached same-ID host fail-closed without mutating semantic state.
  3. Preserve current valid canonical-host target resolution, target ordering, Door/WallOpening category checks and `HostWallId` relation checks.
  4. Add deterministic CAD-independent smoke coverage for canonical success plus detached same-ID and absent-host rejection.
  5. Re-fetch `main` before integration, preserve concurrent work, and close this reservation after source/test review.
- Non-overlap: excludes native opening Boolean execution, Source Reconcile, generated rebar, Build3D, Curtain, Grid, documentation tables/sheets and any currently active V25/runtime lanes. This is only the Core target-state host ownership boundary.
- Validation: source diff + CAD-independent smoke contract review. No GitHub Actions dispatch; no licensed BricsCAD V25 runtime PASS claimed.
- Completion condition: target-state resolution can only trust the exact canonical host object from the supplied `ProjectState`, regression coverage is present, and this claim is marked `DONE`.
