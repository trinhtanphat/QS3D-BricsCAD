# Agent work claim — Semantic Untrack post-commit UI boundary

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-12 (UTC+7)
- Status: `ACTIVE`
- Scope: prevent `QS3DUNTRACK` / `QS3DUNTRACKFINISH` from reporting a committed semantic untrack as a business failure when only palette/editor finalization fails afterward.
- Files reserved:
  - `src/QS3D.BricsCAD.V25/ViewportCommands.cs`
  - `scripts/preflight-untrack-postcommit-ui.py`
  - this claim file
- Contract:
  - selection, existing-project bind, `SemanticUntrackService.Untrack` and its dependency/atomic/revision semantics remain unchanged;
  - mutation/business exceptions are still reported as failures;
  - after `SemanticUntrackService.Untrack` returns successfully, palette refresh/status/editor reporting becomes best-effort and cannot enter the business-failure path;
  - zero-result untrack remains a successful semantic no-op with success-style reporting;
  - zoom/view/model-space commands and native viewport behavior remain untouched;
  - no GitHub Actions dispatch and no BricsCAD V25 runtime PASS from this web session.
