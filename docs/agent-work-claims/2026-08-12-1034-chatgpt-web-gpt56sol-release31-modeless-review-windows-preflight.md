# Work claim — release #31 modeless review windows preflight reconciliation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-release31-modeless-review-windows-preflight`
- Registered: `2026-08-12T10:34:00+07:00`
- Baseline main SHA: `b831401b0def350991e3912c8bc7544ce454476c`
- Priority: release #31 reports `scripts/preflight-modeless-review-windows.py` failing on stale exact source shapes while the modeless source-DWG/current-project guards remain present.

## Reserved scope

Reconcile only `scripts/preflight-modeless-review-windows.py`. Preserve Recognition/BQ/BBS/Revision/Model Health production source unchanged.

## Canonical evidence

- BQ `EnsureCurrentProject(operation)` calls `EnsureActive(operation)`, reads `out var project`, then `EnsureProjectIdentity(project, operation)`; the gate still requires the older `out _` form.
- Recognition manual apply uses nullable `string? firstError = null`, records `ex.Message` on the first failure and passes it to `RefreshStatus`; the gate's old initializer shape is no longer canonical.
- BQ export still guards current project, refreshes current mode before export, and column preference mutation still guards the bound DWG before metadata mutation.
- BBS/Revision/Model Health source-DWG and callback guards remain present.

## Expected surfaces

- `scripts/preflight-modeless-review-windows.py`
- this claim file for close-out

## Excluded scope

- No UI/code-behind edits and no weakening of DWG/project freshness, export recalculation, checked totals or callback ordering.
- No unrelated #31 failures or Actions dispatch.

## Validation plan

- Update BQ helper assertion to require the current project variable and project-identity check after active-DWG validation.
- Update Recognition failure assertion to current nullable first-error shape while retaining catch/message/status requirements.
- Preserve all other review-window safety checks.

## Completion condition

The gate tracks current source shapes without weakening modeless review safety, is pushed to `main`, and this claim is closed with exact implementation evidence.