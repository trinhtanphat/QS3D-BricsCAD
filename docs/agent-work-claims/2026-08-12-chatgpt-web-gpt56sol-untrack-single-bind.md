# Agent work claim — Semantic Untrack read-only target resolve

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-12 (UTC+7)
- Status: `ACTIVE`
- Scope: make `QS3DUNTRACK` / `QS3DUNTRACKFINISH` resolve semantic ownership from read-only project state before canonical mutation binding, no-op zero targets without binding, and revalidate target freshness before the existing Core untrack executor.
- Files reserved:
  - `src/QS3D.BricsCAD.V25/ViewportCommands.cs`
  - `scripts/preflight-untrack-single-bind.py`
  - this claim file
- Contract:
  - preserve existing PICKFIRST-only selection behavior;
  - use `ProjectContextCoordinator.TryGetReadOnly` to resolve ownership/predicate targets before mutation binding;
  - missing project remains a business failure; zero semantic targets becomes the existing successful zero-result/no-op path without `ExistingProjectMutationContext.Require`;
  - freeze preview `ProjectId` + `ChangeVersion` + resolved target IDs;
  - bind canonical project exactly once, fail closed on project/version or target-set drift, then call unchanged `SemanticUntrackService.Untrack`;
  - dependency safety, rollback/revision semantics and post-commit UI isolation remain unchanged;
  - no GitHub Actions dispatch and no BricsCAD V25 runtime PASS from this web session.
