# Quantity Locate Validation Failure Clear Plan

Date: 2026-08-11
Owner: ChatGPT Web / GPT-5.6 Sol
Claim: `docs/agent-work-claims/2026-08-11-2318-chatgpt-web-gpt56sol-quantity-locate-validation-failure-clear.md`
Status: `IMPLEMENTATION_READY_FOR_MERGE`

## Goal

Ensure a quantity locate attempt cannot leave a previous CAD implied selection highlighted when stale-row or stale-project validation fails before the normal explicit `CadHandleService.Select(...)` call.

## Verified residual

The completed stale-selection lane now clears zero-live and zero-candidate targets. A separate failure class remains:

- `QuantitySummaryWindow.LocateCurrent()` performs `EnsureCurrentProject(...)` and `ResolveCurrentRow(...)` before its selection replacement. Exceptions from same-DWG stale project/row validation reach the catch without clearing the previous implied selection.
- `QuantityInsightPanel.LocateSelected()` can reject same-bound-DWG project/row state before its normal target-selection call, leaving the prior implied selection visible.

This can visually associate an old CAD highlight with a newly clicked quantity row that was actually rejected as stale.

## Safety boundary

Selection clearing must be document-affine:

- clear only when the target `Document` is still `MdiActiveDocument`;
- never clear the currently active DWG because a stale panel/window belongs to another DWG;
- clearing is best-effort and must not mask the authoritative locate validation error if BricsCAD refuses a selection update during teardown/document transition;
- no project mutation, persistence, reporting, regeneration or camera behavior changes.

## Implementation refinement

The first plan proposed adding clears directly to canonical failure returns/catches. Current `main` has kept both canonical Quantity code-behind files stable while many agents are committing elsewhere, and rewriting either full high-churn WPF file through the connector would create unnecessary stale-tree/collision risk.

The implementation therefore uses two source-stable partial-class guards instead:

1. `QuantitySummaryWindow.LocateSelectionFailureGuard.cs` registers WPF class handlers for the exact existing Summary locate triggers: the unique `Định vị` button, `QuantityGrid` selection changes that satisfy current detail/AutoReveal conditions, and the existing double-click path.
2. `QuantityInsightPanel.LocateSelectionFailureGuard.cs` registers class handlers for the exact existing Insight locate triggers: the unique `Định vị` button, `QuantityTree.SelectedItemChanged` with AutoReveal enabled, and the existing double-click path with AutoReveal disabled.
3. Each partial uses an **explicit static constructor** so class-handler registration is guaranteed before instance initialization rather than relying on CLR `beforefieldinit` timing.
4. Each handler resolves the exact owning Quantity surface and calls a tiny best-effort helper **before the existing instance locate handler**. The helper rechecks document affinity and clears through `CadHandleService.Select(document, Array.Empty<string>())` only when that bound document is still `MdiActiveDocument`.
5. A successful canonical locate immediately revalidates and selects the intended live target again; a validation failure leaves the exact active DWG unselected rather than preserving an unrelated earlier highlight.
6. Wrong-DWG clicks remain non-clearing because the helper's `ReferenceEquals(MdiActiveDocument, boundDocument)` check fails.
7. Canonical code-behind/XAML, reporting/persistence semantics, zero-candidate behavior and positive-count-only `QS3DZOOMSELECTED` ordering remain unchanged.

This refinement is deliberately recorded before integration of the source branch into `main`.

## Regression gate

`preflight-quantity-locate-validation-failure-clear.py` locks:

- SDK-style WPF/default compile inclusion for the new partial files;
- explicit static constructors and rejection of delayed static-field registration;
- exact class-handler trigger types and current XAML event wiring;
- exact Summary/Insight ownership filtering;
- active-document check before `Select(empty)`;
- no project mutation/bootstrap/touch/zoom logic in guards;
- unchanged canonical validated-target selection and zoom gating.

## Verification

- prior Quantity merge to implementation checkpoint was compared across 171 commits with no edits to the two canonical locate files;
- branch base to then-current `main` was compared across another 105 commits with no overlap on the two canonical locate files, XAML locate wiring, V25 project file, guard paths or focused gate;
- branch diff is exactly three new files: two partial guards plus one focused preflight;
- parse/inspect the focused preflight and source contracts;
- open/merge through GitHub with expected head SHA and no force update;
- re-fetch merged files by merge SHA and compare post-merge `main` for overwrite/collision;
- record status/workflow absence as absence, not CI PASS.

Licensed BricsCAD V25 interaction remains `PENDING_LOCAL / DO_NOT_RETRY_REMOTE` under the existing local qualification queue.
