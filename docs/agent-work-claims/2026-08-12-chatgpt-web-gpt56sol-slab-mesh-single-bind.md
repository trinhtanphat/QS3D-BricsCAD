# Agent work claim — Slab Mesh 3D read-only target resolve

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-12 (UTC+7)
- Status: `ACTIVE`
- Scope: make `QS3DSLABREBAR3D` resolve selected semantic Slab targets from read-only project state before canonical mutation binding, then revalidate project/target freshness before the unchanged native builder; reconcile any directly related stale Slab atomicity gate if it still expects standalone batch revision touches.
- Files reserved:
  - `src/QS3D.BricsCAD.V25/SlabMeshCommands.cs`
  - `scripts/preflight-slab-mesh-single-bind.py`
  - directly related Slab mesh atomicity preflight only if stale
  - this claim file
- Contract:
  - preserve `CadSelectionGuard.AcquireCurrentSelection` PICKFIRST/interactive handoff;
  - resolve `ElementCategory.Slab` targets whose `SourceHandles` intersect selected CAD handles using `TryGetReadOnly` before mutation binding;
  - missing project or zero targets returns without `ExistingProjectMutationContext.Require`;
  - freeze preview `ProjectId` + `ChangeVersion` + target IDs, bind canonical state exactly once, fail closed on drift, then call unchanged `SlabMeshSolidBuilder.BuildSelected`;
  - preserve rectangular/polygon planning, native ownership, bar limits, transaction/rollback/audit and post-commit UI;
  - no GitHub Actions dispatch and no BricsCAD V25 runtime PASS from this web session.
