# Agent work claim — Source Reconcile read-only target resolve

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-12 (UTC+7)
- Status: `ACTIVE`
- Scope: validate `QS3DSYNCSOURCE` selected ownership against read-only project state before canonical mutation binding, revalidate target freshness after one bind, and reconcile the aggregate Source Reconcile gate with the already-completed AuditTrail-owned revision contract.
- Files reserved:
  - `src/QS3D.BricsCAD.V25/Services/SourceReconcileService.cs`
  - `scripts/preflight-source-reconcile.py`
  - `scripts/preflight-source-reconcile-single-bind.py`
  - this claim file
- Contract:
  - selection remains first; empty/cancel remains side-effect free;
  - require an existing read-only project and run existing generated/source ownership validation before canonical mutation binding;
  - invalid generated/untracked/ambiguous/non-P0 selections fail before `ExistingProjectMutationContext.Require`;
  - freeze preview `ProjectId` + `ChangeVersion` + resolved target IDs;
  - bind canonical project exactly once, fail closed on project/version/target-set drift, then keep existing invalidation/regeneration/CAD transaction/rollback behavior;
  - preserve per-target `source.reconcile` AuditTrail as revision owner with no standalone `project.Touch()`;
  - update `preflight-source-reconcile.py`, which still expects the removed standalone Touch, without weakening its ownership/performance/rollback/native-boundary checks;
  - no GitHub Actions dispatch and no BricsCAD V25 runtime PASS from this web session.
