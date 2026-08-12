# Work claim — release #31 modeless review windows preflight reconciliation

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-release31-modeless-review-windows-preflight`
- Registered: `2026-08-12T10:34:00+07:00`
- Completed: `2026-08-12T10:36:00+07:00`
- Baseline main SHA: `b831401b0def350991e3912c8bc7544ce454476c`
- Claim commit: `60b0f52cd290a6d93d99831f738018d0bfdcc87c`
- Implementation commit: `e7c5e5fbb5b6cccfeff910b0e94a867ed556a177`
- Priority: release #31 reports `scripts/preflight-modeless-review-windows.py` failing on stale exact source shapes while the modeless source-DWG/current-project guards remain present.

## Completed reconciliation

- BQ `EnsureCurrentProject(operation)` is now gated as active-DWG check -> existing project read -> `EnsureProjectIdentity(project, operation)`.
- Recognition manual apply is gated against the current nullable first-error flow, including catch, first `ex.Message` capture and final `RefreshStatus(applied, failed, firstError)` propagation.
- Existing BQ preference/export, BBS checked-total, Revision callback and Model Health callback assertions remain intact.
- No production UI/code-behind source changed.

## Validation boundary

Current-main gate/source readback only. GitHub Actions was not dispatched and no build, smoke, signing, package or licensed BricsCAD V25/V26 runtime PASS is claimed.

## Completion condition

Completed by implementation `e7c5e5fbb5b6cccfeff910b0e94a867ed556a177`.