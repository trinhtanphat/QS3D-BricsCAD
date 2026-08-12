# Agent work claim — Beam Stirrup 3D read-only target resolve

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-12 (UTC+7)
- Status: `ACTIVE`
- Scope: make `QS3DREBARSTIRRUP3D` / workspace alias resolve selected semantic Beam targets from read-only project state before canonical mutation binding, then revalidate project/target freshness before the unchanged native builder.
- Files reserved:
  - `src/QS3D.BricsCAD.V25/BeamStirrupCommands.cs`
  - `scripts/preflight-beam-stirrup-single-bind.py`
  - this claim file
- Contract:
  - preserve command aliases, health commands and `CadSelectionGuard.AcquireCurrentSelection(document)` PICKFIRST/interactive handoff;
  - resolve `ElementCategory.Beam` targets whose `SourceHandles` intersect selected CAD handles using `ProjectContextCoordinator.TryGetReadOnly` before mutation binding;
  - missing project or zero semantic Beam targets returns without `ExistingProjectMutationContext.Require`;
  - freeze preview `ProjectId` + `ChangeVersion` + target IDs;
  - bind canonical project exactly once, fail closed on project/version/target-set drift, then call unchanged `BeamStirrupSolidBuilder.BuildSelected`;
  - preserve RebarStirrupNotation/layout/hook geometry, native ownership, batch limits, transaction/rollback/audit and post-commit UI;
  - no GitHub Actions dispatch and no BricsCAD V25 runtime PASS from this web session.
