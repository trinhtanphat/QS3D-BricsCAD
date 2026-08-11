# Quantity Locate Stale Selection Clear Plan

Date: 2026-08-11
Owner: ChatGPT Web / GPT-5.6 Sol
Claim: `docs/agent-work-claims/2026-08-11-chatgpt-web-gpt56sol-quantity-locate-stale-selection-clear.md`
Status: `IMPLEMENTATION_PENDING`

## Goal

Ensure every quantity locate attempt replaces the CAD implied selection with the target row's currently live objects. If no target object survives, the implied selection must become empty so the previous row cannot remain highlighted and be mistaken for the failed target.

## Verified defect

`CadHandleService.SelectIfAny(...)` resolves candidate handles and returns `0` immediately when none are live. In that branch it never calls `Editor.SetImpliedSelection(...)`. Both `QuantitySummaryWindow` and `QuantityInsightPanel` use this select behavior for viewport locate. Therefore a successful locate of row A followed by a locate of row B whose handles are all stale/erased can leave row A visibly selected even though row B reports zero selected objects.

This is a presentation/safety defect, not a reporting defect. Grouped rows already union all `ElementIds` and `SourceHandles`, and `CadHandleService.Resolve(...)` already preserves all surviving multi-object IDs while skipping invalid/erased handles individually.

## Coordination boundary

The completed `quantity-description-3d-locate` and `quantity-insight-single-click-reveal` lanes remain authoritative for their existing UX and stale-row revalidation. This follow-up does not reopen their reporting/identity logic. No generic behavior change will be made to existing `Select(...)`/`SelectIfAny(...)` because unrelated callers may intentionally rely on the current no-op-on-empty semantics.

## Implementation

### 1. Add explicit replacement-selection primitive

In `CadHandleService` add a narrowly named method for callers that need replacement semantics:

- resolve handles with the existing `Resolve(...)` implementation;
- always call `document.Editor.SetImpliedSelection(...)`, including when the resolved ID list is empty;
- return the number of live selected IDs;
- do not change `Resolve`, `Select`, `SelectIfAny`, `GetLiveHandles`, or `GetLiveSolidHandles` behavior.

### 2. QS3DBQ locate

In `QuantitySummaryWindow.LocateCurrent()`:

- use the replacement-selection primitive for the revalidated `SourceHandles` path;
- preserve partial stale-handle behavior and `selected / expected` status;
- when selected count is zero, status remains fail-closed and the previous implied selection is now cleared;
- retain `QS3DZOOMSELECTED` only after a positive selection;
- keep semantic-first/source-handle fallback revalidation unchanged.

### 3. Quantity Insight locate

In `QuantityInsightPanel.LocateSelected()`:

- use the same replacement-selection primitive after current-row revalidation;
- if semantic handle resolution produces no candidates, explicitly replace with an empty selection before returning so an earlier reveal cannot remain highlighted;
- report zero-live-target status clearly;
- preserve current project/DWG affinity, detached-preview row revalidation and positive-count-only zoom.

## Focused regression gate

Add `scripts/preflight-quantity-locate-stale-selection-clear.py` verifying:

- `CadHandleService` exposes the new replacement primitive;
- it calls `Resolve(...)` and then `SetImpliedSelection(...)` unconditionally, rather than returning before the selection replacement;
- existing `SelectIfAny` keeps its existing behavior to prevent unrelated scope expansion;
- both QuantitySummary and QuantityInsight use replacement semantics;
- Quantity Insight explicitly clears via replacement semantics on the no-candidate branch;
- both locate paths keep zoom after positive-count guards;
- no project mutation/bootstrap calls are introduced into the quantity locate blocks.

## Verification

Remote/source checks:

1. Re-fetch all modified files from merged `main`.
2. Parse the focused Python gate for syntax and inspect its source contracts.
3. Confirm implementation diff stays inside the registered scope and does not alter reporting/persistence semantics.
4. Check current-main concurrency between implementation/gate commits and final qualification.
5. Inspect GitHub status/workflow records; absence is recorded, not treated as CI PASS.

Native V25 qualification:

- locate a valid quantity row A;
- delete/erase all CAD objects represented by target row B without leaving valid handles, then activate B and verify A is no longer PICKFIRST-highlighted;
- test a partially stale grouped row and verify surviving objects replace the previous selection, status reports partial selection, and zoom frames only survivors;
- repeat in Quantity Insight with auto-reveal enabled;
- confirm multi-DWG stale panel/window protection remains fail-closed.

This licensed interactive proof remains `PENDING_LOCAL / DO_NOT_RETRY_REMOTE` under the existing local qualification queue; no duplicate local inbox item is required unless current source changes make the existing queue insufficient.

## Completion criteria

Source-side work is complete when both quantity locate surfaces replace rather than preserve the prior implied selection, empty-target locate visibly clears stale CAD highlight, partial/multi-object behavior remains intact, focused regression coverage is committed, and the work claim records exact merged evidence.
