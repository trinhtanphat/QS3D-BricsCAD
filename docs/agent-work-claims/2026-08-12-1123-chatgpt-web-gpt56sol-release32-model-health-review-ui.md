# Work claim — release #32 Model Health review UI preflight reconciliation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-release32-model-health-review-ui`
- Registered: `2026-08-12T11:23:00+07:00`
- Baseline main SHA: `0a075eacbb9781bd4a782caaa17499abd8f061f4`
- Priority: release #32 reports `scripts/preflight-model-health-review-ui.py` failing on a stale combined footer/header copy token.

## Reserved scope

Reconcile only `scripts/preflight-model-health-review-ui.py`; preserve ModelHealthWindow XAML/code-behind unchanged.

## Canonical evidence

- Current XAML remains well-formed and exposes HEALTH REVIEW, search, severity filters, visible count, issue grid, locate click and double-click.
- Current premium layout deliberately renders `READ-ONLY TRIAGE` and `ISSUE → CAD LOCATE` as separate status pills instead of the obsolete single literal `READ-ONLY TRIAGE • ISSUE → CAD LOCATE`.
- Code-behind still keeps in-memory filtering, project/DWG freshness identity, stale disablement and guarded locate callback; no mutation/health recomputation is performed by the window.

## Contract

The gate must require both current read-only/locate UI markers independently while preserving all search/filter/freshness/read-only assertions and forbidden mutation checks.

## Excluded scope

No XAML/code-behind edits, no UI redesign, no health-service changes and no unrelated #32 work. No Actions/build/runtime PASS claim.

## Completion condition

The gate tracks the current split premium UI markers without weakening Model Health review safety, is read back on current `main`, and this claim is closed with exact evidence.