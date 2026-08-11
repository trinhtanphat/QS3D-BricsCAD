# Quantity Locate Stale Selection Clear Plan

Date: 2026-08-11
Owner: ChatGPT Web / GPT-5.6 Sol
Claim: `docs/agent-work-claims/2026-08-11-chatgpt-web-gpt56sol-quantity-locate-stale-selection-clear.md`
Status: `FOLLOWUP_IMPLEMENTATION_PENDING`

## Goal

Ensure every explicit quantity locate attempt replaces the CAD implied selection with the target row's currently live objects. If no target object survives—or no candidate handle can be resolved at all—the implied selection must become empty so the previous row cannot remain highlighted and be mistaken for the failed target.

## Verified defect and first fix

The initial defect was that `CadHandleService.Select(...)` delegated to `SelectIfAny(...)`, whose zero-live-ID branch preserves the existing implied selection. This was fixed in `e9df086f6a4cdcb66edd8fa0d7a12717e4bf5308`: explicit `Select(...)` now resolves the handles and always calls `Editor.SetImpliedSelection(...)`, including with an empty resolved ObjectId set. `SelectIfAny(...)` intentionally retains its preserve-on-empty semantics. The initial focused gate was merged as `ad52d6b2a27ab102cc87f20711a031d975665440`.

Qualification then exposed a second branch that bypasses the corrected API entirely:

- `QuantityInsightPanel.LocateSelected()` returns immediately when `SourceHandleResolver.Resolve(...)` yields zero candidate handles.
- `QuantitySummaryWindow.LocateCurrent()` reaches its fallback callback when the revalidated row has zero `SourceHandles`; that callback can also return before selection replacement.

Both branches can therefore leave an earlier quantity row highlighted even though the new target is not locatable.

## Coordination boundary

The completed quantity-description and Quantity Insight reveal lanes remain authoritative for stale-row validation, source-handle fallback, auto-reveal and viewport behavior. `Commands.cs` is deliberately not modified in this follow-up because it is under active/high-frequency concurrent changes. Clearing the selection in `QuantitySummaryWindow` before invoking the existing callback closes the visual-staleness defect without touching that shared command surface.

## Implementation

### 1. Explicit `Select(...)` replacement semantics — already merged

Keep the current `CadHandleService` contract:

- `Select(...)`: resolve all live IDs, always replace implied selection, return live count.
- `SelectIfAny(...)`: return zero without changing selection when none survive; otherwise replace.
- `Resolve(...)`: keep case-insensitive normalized-handle deduplication and independent stale/erased filtering.

### 2. Quantity Summary zero-candidate path

In `QuantitySummaryWindow.LocateCurrent()`:

- retain the existing positive-`liveHandles` flow unchanged;
- on the branch reached only when `liveHandles.Length == 0`, call `Cad.CadHandleService.Select(_document, liveHandles)` before invoking `_locate` or reporting no handle;
- because `liveHandles` is empty on this branch, the explicit `Select(...)` call clears any previous implied selection;
- keep the existing callback available for compatibility; if it finds another target it can select anew, otherwise the cleared state remains authoritative;
- do not alter semantic-first/source-handle row revalidation, statuses, partial-selection behavior or zoom ordering.

### 3. Quantity Insight zero-candidate path

In `QuantityInsightPanel.LocateSelected()`:

- when `SourceHandleResolver.Resolve(...)` returns zero handles, call `Cad.CadHandleService.Select(document, handles)` before setting the existing non-locatable status and returning;
- keep the normal positive-candidate `Select(...)` path and positive-count-only `QS3DZOOMSELECTED` behavior unchanged;
- keep bound-DWG/project identity and detached-preview row validation unchanged.

### 4. Extend focused regression gate

Update `scripts/preflight-quantity-locate-stale-selection-clear.py` to verify:

- explicit `Select` remains unconditional and `SelectIfAny` remains preserve-on-empty;
- `QuantitySummaryWindow.LocateCurrent()` contains two explicit `Select(...)` calls: one for positive live handles and one on the zero-candidate branch before `_locate` fallback;
- `QuantityInsightPanel.LocateSelected()` contains an explicit `Select(...)` inside the `handles.Count == 0` branch before status/return, plus the normal selection call afterward;
- both paths retain positive-selection-only zoom;
- no project mutation/bootstrap calls are introduced into either locate method;
- `Resolve(...)` multi-object dedup and stale/erased filtering remain intact.

## Verification

Remote/source checks:

1. Re-fetch each modified WPF file and the gate from merged `main`.
2. Parse the focused Python gate for syntax and inspect ordering contracts.
3. Confirm the follow-up diff touches only the two registered WPF locate surfaces plus focused gate/docs; no reporting/persistence or `Commands.cs` changes.
4. Check current-main concurrency immediately before merge and again after qualification; do not force-push over concurrent work.
5. Inspect GitHub status/workflow records; absence is recorded rather than treated as CI PASS.

Native V25 qualification:

- locate a valid row A, then activate a row B with no semantic/source handle candidate and verify A's PICKFIRST highlight clears;
- repeat when candidate handle strings exist but all native objects are stale/erased;
- test a partially stale grouped row and verify surviving objects replace the previous selection and zoom frames only survivors;
- repeat in Quantity Insight with auto-reveal enabled;
- confirm multi-DWG stale panel/window protection remains fail-closed.

Licensed interactive proof remains `PENDING_LOCAL / DO_NOT_RETRY_REMOTE` under the existing local qualification queue; no duplicate local inbox item is needed.

## Completion criteria

Source-side work is complete when both zero-live-ID and zero-candidate quantity locate outcomes clear any previous implied selection, partial/multi-object behavior remains intact, `SelectIfAny(...)` preserves its original contract, focused regression coverage is updated, and the claim records exact merged evidence.
