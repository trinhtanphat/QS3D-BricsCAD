# Quantity Locate Validation Failure Clear Plan

Date: 2026-08-11
Owner: ChatGPT Web / GPT-5.6 Sol
Claim: `docs/agent-work-claims/2026-08-11-2318-chatgpt-web-gpt56sol-quantity-locate-validation-failure-clear.md`
Status: `IMPLEMENTATION_PENDING`

## Goal

Ensure a quantity locate attempt cannot leave a previous CAD implied selection highlighted when stale-row or stale-project validation fails before the normal explicit `CadHandleService.Select(...)` call.

## Verified residual

The completed stale-selection lane now clears zero-live and zero-candidate targets. A separate failure class remains:

- `QuantitySummaryWindow.LocateCurrent()` performs `EnsureCurrentProject(...)` and `ResolveCurrentRow(...)` before its selection replacement. Exceptions from same-DWG stale project/row validation reach the catch without clearing the previous implied selection.
- `QuantityInsightPanel.LocateSelected()` returns before its locate `try` when the same bound DWG no longer has the expected project identity, and exceptions from `ResolveCurrentRow(...)` likewise report failure without clearing the prior implied selection.

This can visually associate an old CAD highlight with a newly clicked quantity row that was actually rejected as stale.

## Safety boundary

Selection clearing must be document-affine:

- clear only when the target `Document` is still `MdiActiveDocument`;
- never clear the currently active DWG because a stale panel/window belongs to another DWG;
- clearing is best-effort and must not mask the original validation error if BricsCAD refuses a selection update during teardown/document transition;
- no project mutation, persistence, reporting, regeneration or camera behavior changes.

## Implementation

1. Add a tiny best-effort helper in each quantity UI surface (or an equivalent local helper) that rechecks active-document identity and calls `CadHandleService.Select(document, Array.Empty<string>())` only for that exact active document.
2. `QuantitySummaryWindow`: invoke the helper in the locate exception path. Because it rechecks document affinity, a wrong-DWG `EnsureActive(...)` failure does not clear another document.
3. `QuantityInsightPanel`: invoke the helper before same-bound-DWG project-unavailable/project-identity failure returns and in the locate exception path. Keep wrong-DWG return unchanged and non-clearing.
4. Preserve the already-implemented zero-candidate selection clear, normal selection, partial stale handling and positive-count-only `QS3DZOOMSELECTED` ordering.
5. Add an auto-discovered static preflight locking the helper's document-affinity check and the exact failure-path ordering while rejecting project mutation/bootstrap calls.

## Verification

- re-fetch exact source blobs before editing;
- compare current main against the prior locate merge to detect concurrent edits;
- parse/inspect the focused preflight;
- re-fetch merged source by merge SHA;
- compare post-merge main commits for overwrite/collision;
- record status/workflow absence as absence, not CI PASS.

Licensed BricsCAD V25 interaction remains `PENDING_LOCAL / DO_NOT_RETRY_REMOTE` under the existing local qualification queue.
