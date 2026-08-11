# Agent work claim — Shape Rebar 3D read-only target resolve

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-12 (UTC+7)
- Status: `ACTIVE`
- Scope: make `QS3DREBAR3DSHAPE` resolve selected semantic elements with non-empty `RebarNotation` from read-only project state before canonical mutation binding, then revalidate project/target freshness before the unchanged native builder; reconcile the existing Shape atomicity gate with the builder's current AuditTrail-owned revision contract.
- Files reserved:
  - `src/QS3D.BricsCAD.V25/ShapeRebarGeometryCommands.cs`
  - `scripts/preflight-shape-rebar-atomicity.py`
  - `scripts/preflight-shape-rebar-single-bind.py`
  - this claim file
- Contract:
  - preserve `CadSelectionGuard.AcquireCurrentSelection` interactive/PICKFIRST handoff and implied-selection behavior;
  - derive selected source handles and resolve builder-eligible semantic elements (`SourceHandles` match + non-empty `RebarNotation`) against `TryGetReadOnly` before mutation binding;
  - missing project or zero eligible targets returns without `ExistingProjectMutationContext.Require`;
  - freeze preview `ProjectId` + `ChangeVersion` + target IDs;
  - bind canonical project exactly once, fail closed on project/version/target-set drift, then call unchanged `ShapeRebarSolidBuilder.BuildSelected`;
  - builder ownership, BBS schedule, native transaction/rollback/audit and post-commit UI behavior remain unchanged;
  - update the stale `preflight-shape-rebar-atomicity.py` expectation from standalone batch `project.Touch()` to per-element `geometry.rebar.shape` AuditTrail-owned revision, without weakening native ownership/transaction/rollback checks;
  - no GitHub Actions dispatch and no BricsCAD V25 runtime PASS from this web session.
