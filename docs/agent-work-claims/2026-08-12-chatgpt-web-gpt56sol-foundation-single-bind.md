# Agent work claim — Foundation Rebar 3D read-only target resolve

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-12 (UTC+7)
- Status: `ACTIVE`
- Scope: make `QS3DFOUNDATIONREBAR3D` resolve selected semantic Foundation targets from read-only project state before canonical mutation binding, then revalidate project/target freshness before the unchanged native builder.
- Files reserved:
  - `src/QS3D.BricsCAD.V25/FoundationMeshCommands.cs`
  - `scripts/preflight-foundation-mesh-single-bind.py`
  - this claim file
- Contract:
  - preserve `CadSelectionGuard.AcquireCurrentSelection` PICKFIRST/interactive handoff and implied-selection behavior;
  - derive selected CAD handles and resolve `ElementCategory.Foundation` elements whose `SourceHandles` intersect selection against `ProjectContextCoordinator.TryGetReadOnly` before mutation binding;
  - missing project or zero semantic Foundation targets returns without `ExistingProjectMutationContext.Require`;
  - freeze preview `ProjectId` + `ChangeVersion` + target IDs;
  - bind canonical project exactly once, fail closed on project/version/target-set drift, then call unchanged `FoundationMeshSolidBuilder.BuildSelected`;
  - builder rectangle/polygon geometry, native ownership, bar limits, transaction/rollback/audit and post-commit UI remain unchanged;
  - no GitHub Actions dispatch and no BricsCAD V25 runtime PASS from this web session.
