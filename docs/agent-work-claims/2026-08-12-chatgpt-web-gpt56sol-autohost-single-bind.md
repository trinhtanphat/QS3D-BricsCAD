# Agent work claim — Auto Host read-only target resolve

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-12 (UTC+7)
- Status: `ACTIVE`
- Scope: resolve selected Door/WallOpening targets from read-only project state before `QS3DAUTOLINKHOSTS` canonical mutation binding, then revalidate project/target freshness after exactly one bind.
- Files reserved:
  - `src/QS3D.BricsCAD.V25/AutoHostLinkCommands.cs`
  - `scripts/preflight-auto-host-project-lifecycle.py`
  - `scripts/preflight-autohost-single-bind.py`
  - this claim file
- Contract:
  - preserve current PICKFIRST/interactive selection acquisition and existing-project-only semantics;
  - use `ProjectContextCoordinator.TryGetReadOnly` to resolve selected Door/WallOpening targets before mutation binding;
  - missing project or zero semantic Opening targets returns without `ExistingProjectMutationContext.TryGet`;
  - freeze preview `ProjectId` + `ChangeVersion` + selected Opening IDs;
  - obtain canonical mutation project exactly once, fail closed on project/version/target-set drift, then preserve matching, ambiguity/unmatched handling, metadata updates, rollback, scoped regeneration and post-commit UI isolation;
  - `LinkSingleOpening` remains unchanged;
  - reconcile existing Auto Host lifecycle gate and add a focused single-bind guard;
  - no GitHub Actions dispatch and no BricsCAD V25 runtime PASS from this web session.
