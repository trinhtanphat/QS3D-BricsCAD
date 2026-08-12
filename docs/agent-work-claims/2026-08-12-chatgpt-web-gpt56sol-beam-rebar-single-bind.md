# Agent work claim — Beam Rebar 3D read-only target resolve

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-12 (UTC+7)
- Status: `ACTIVE`
- Scope: make `QS3DBEAMREBAR3D` resolve selected semantic Beam targets from read-only project state before canonical mutation binding, then revalidate project/target freshness before the unchanged native builder.
- Files reserved:
  - `src/QS3D.BricsCAD.V25/BeamRebarCommands.cs`
  - `scripts/preflight-beam-rebar-single-bind.py`
  - this claim file
- Contract:
  - preserve `CadSelectionGuard.AcquireCurrentSelection(document)` PICKFIRST/interactive handoff and implied-selection behavior;
  - resolve `ElementCategory.Beam` targets whose `SourceHandles` intersect selected CAD handles using `ProjectContextCoordinator.TryGetReadOnly` before mutation binding;
  - missing project or zero semantic Beam targets returns without `ExistingProjectMutationContext.Require`;
  - freeze preview `ProjectId` + `ChangeVersion` + target IDs;
  - bind canonical project exactly once, fail closed on project/version/target-set drift, then call unchanged `BeamRebarSolidBuilder.BuildSelected`;
  - preserve Beam LINE/RebarNotation/top-bottom validation, native ownership, batch limits, transaction/rollback/audit and post-commit UI;
  - no GitHub Actions dispatch and no BricsCAD V25 runtime PASS from this web session.
