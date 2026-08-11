# Agent work claim — Selected Opening Boolean single canonical bind

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-12 (UTC+7)
- Status: `ACTIVE`
- Scope: make `QS3DCUTSELECTEDOPENINGS` resolve its selected Door/WallOpening targets from read-only project state, no-op before mutation binding when there are no valid targets, and bind the canonical mutation project exactly once for a valid cut.
- Files reserved:
  - `src/QS3D.BricsCAD.V25/OpeningBooleanCommands.cs`
  - `scripts/preflight-opening-selected-single-bind.py`
  - this claim file
- Contract:
  - acquire PICKFIRST/interactive snapshots before any mutation-project bind;
  - resolve selected semantic Opening/Door ids against `TryGetReadOnly` preview state;
  - missing project or zero resolved targets returns without `ExistingProjectMutationContext.Require`;
  - freeze preview `ProjectId` + `ChangeVersion`, then canonical bind exactly once for a valid target set;
  - after bind, fail closed on project/version drift and re-resolve the selected source handles to ensure the target-id set is unchanged;
  - pass the already-bound canonical project into the shared physical-cut path instead of binding again;
  - `QS3DCUTOPENINGS` all-opening behavior, OpeningBoolean service/guard internals, audit ownership and native geometry semantics are unchanged;
  - no GitHub Actions dispatch and no BricsCAD V25 runtime PASS from this web session.
