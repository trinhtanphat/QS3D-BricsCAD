# Quantity Locate Stale Selection Clear Plan

Date: 2026-08-11
Owner: ChatGPT Web / GPT-5.6 Sol
Claim: `docs/agent-work-claims/2026-08-11-chatgpt-web-gpt56sol-quantity-locate-stale-selection-clear.md`
Status: `IMPLEMENTATION_PENDING`

## Goal

Ensure every explicit quantity locate attempt replaces the CAD implied selection with the target row's currently live objects. If no target object survives, the implied selection must become empty so the previous row cannot remain highlighted and be mistaken for the failed target.

## Verified defect

`CadHandleService.Select(...)` currently delegates directly to `SelectIfAny(...)`. `SelectIfAny(...)` resolves candidate handles and returns `0` immediately when none are live, so it intentionally does not call `Editor.SetImpliedSelection(...)` in the empty case. Both `QuantitySummaryWindow` and `QuantityInsightPanel` use `CadHandleService.Select(...)` for viewport locate. Therefore a successful locate of row A followed by a locate of row B whose handles are all stale/erased can leave row A visibly selected even though row B reports zero selected objects.

This is a selection-contract defect, not a reporting defect. Grouped rows already union all `ElementIds` and `SourceHandles`, and `CadHandleService.Resolve(...)` already preserves all surviving multi-object IDs while skipping invalid/erased handles individually.

## Coordination boundary

The completed `quantity-description-3d-locate` and `quantity-insight-single-click-reveal` lanes remain authoritative for their existing UX and stale-row revalidation. The implementation refinement deliberately avoids editing those high-churn WPF files: both already call the explicit `Select(...)` API. No change is made to `SelectIfAny(...)`, which remains the preserve-existing-selection-on-empty primitive for callers that request that behavior.

## Implementation

### 1. Correct explicit `Select(...)` semantics

In `CadHandleService`:

- keep `Resolve(...)` unchanged;
- change `Select(...)` from an alias of `SelectIfAny(...)` into an explicit replacement operation;
- resolve all handles using the existing `Resolve(...)` logic;
- always call `document.Editor.SetImpliedSelection(...)`, including when the resolved ID list is empty;
- return the number of live selected IDs;
- keep `SelectIfAny(...)` byte-for-byte behaviorally equivalent: resolve, return zero without touching implied selection when empty, otherwise set the live IDs;
- keep `GetLiveHandles(...)` and `GetLiveSolidHandles(...)` unchanged.

This gives the two API names distinct, predictable contracts: `Select` replaces the implied selection; `SelectIfAny` replaces only when at least one target survives.

### 2. Quantity surfaces remain source-stable

`QuantitySummaryWindow.LocateCurrent()` and `QuantityInsightPanel.LocateSelected()` already call `CadHandleService.Select(...)`. Do not rewrite those files unless current `main` removes that call before merge. Their existing zero-count status and positive-count-only `QS3DZOOMSELECTED` guards then inherit the corrected replacement behavior automatically.

### 3. Focused regression gate

Add `scripts/preflight-quantity-locate-stale-selection-clear.py` verifying:

- `CadHandleService.Select(...)` calls `Resolve(...)` and then `SetImpliedSelection(...)` unconditionally;
- `Select(...)` contains no zero-count early return before selection replacement;
- `SelectIfAny(...)` still contains `if (ids.Count == 0) return 0;` before `SetImpliedSelection(...)`;
- `QuantitySummaryWindow` and `QuantityInsightPanel` still call `Cad.CadHandleService.Select(...)` / `CadHandleService.Select(...)` in their locate flows;
- both quantity paths keep `QS3DZOOMSELECTED` after positive-selection guards;
- the service still deduplicates normalized handles and filters invalid/erased entities through the existing `Resolve(...)` path;
- no project mutation/bootstrap behavior is introduced.

## Verification

Remote/source checks:

1. Re-fetch `CadHandleService.cs` and both Quantity locate source files from merged `main`.
2. Parse the focused Python gate for syntax and inspect its source contracts.
3. Confirm the implementation diff changes only `CadHandleService.cs` plus the focused gate/docs; no reporting/persistence semantics.
4. Check current-main concurrency between implementation/gate commits and final qualification.
5. Inspect GitHub status/workflow records; absence is recorded, not treated as CI PASS.

Native V25 qualification:

- locate valid quantity row A;
- delete/erase all CAD objects represented by target row B without leaving valid handles, then activate B and verify A is no longer PICKFIRST-highlighted;
- test a partially stale grouped row and verify surviving objects replace the previous selection, status reports partial/selected count as implemented, and zoom frames only survivors;
- repeat in Quantity Insight with auto-reveal enabled;
- confirm a deliberate `SelectIfAny(...)` caller still preserves the old implied selection when given no live target;
- confirm multi-DWG stale panel/window protection remains fail-closed.

This licensed interactive proof remains `PENDING_LOCAL / DO_NOT_RETRY_REMOTE` under the existing local qualification queue; no duplicate local inbox item is required unless current source changes make the existing queue insufficient.

## Completion criteria

Source-side work is complete when explicit `Select(...)` replaces rather than preserves the prior implied selection, empty-target quantity locate visibly clears stale CAD highlight, `SelectIfAny(...)` preserves its original contract, partial/multi-object behavior remains intact, focused regression coverage is committed, and the work claim records exact merged evidence.
