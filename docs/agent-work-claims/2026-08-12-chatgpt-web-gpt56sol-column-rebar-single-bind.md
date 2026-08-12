# Agent work claim — Column Rebar 3D read-only target resolve

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-12 (UTC+7)
- Status: `ACTIVE`
- Scope: make PICKFIRST-only `QS3DREBAR3D` resolve selected semantic Column targets from read-only project state before canonical mutation binding, then revalidate project/target freshness before the unchanged native builder.
- Files reserved:
  - `src/QS3D.BricsCAD.V25/RebarGeometryCommands.cs`
  - `scripts/preflight-column-rebar-single-bind.py`
  - this claim file
- Contract:
  - preserve `CadSelectionGuard.ReadImpliedSelection(document)` and PICKFIRST-only UX; do not add interactive `GetSelection`/`AcquireCurrentSelection`;
  - resolve `ElementCategory.Column` targets whose `SourceHandles` intersect implied CAD handles using `ProjectContextCoordinator.TryGetReadOnly` before mutation binding;
  - missing project or zero semantic Column targets returns without `ExistingProjectMutationContext.Require`;
  - freeze preview `ProjectId` + `ChangeVersion` + target IDs;
  - bind canonical project exactly once, fail closed on project/version/target-set drift, then call unchanged `ColumnRebarSolidBuilder.BuildSelected`;
  - preserve rectangle/planarity/RebarNotation validation, native ownership, batch limits, transaction/rollback/audit and post-commit UI;
  - no GitHub Actions dispatch and no BricsCAD V25 runtime PASS from this web session.
