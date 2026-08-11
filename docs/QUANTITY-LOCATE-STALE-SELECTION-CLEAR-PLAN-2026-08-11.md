# Quantity Locate Stale Selection Clear Plan

Date: 2026-08-11
Owner: ChatGPT Web / GPT-5.6 Sol
Claim: `docs/agent-work-claims/2026-08-11-chatgpt-web-gpt56sol-quantity-locate-stale-selection-clear.md`
Status: `IMPLEMENTED_SOURCE_SIDE`

## Goal

Ensure every explicit quantity locate attempt replaces the CAD implied selection with the target row's currently live objects. If no target object survives—or no candidate handle can be resolved at all—the implied selection becomes empty so the previous row cannot remain highlighted and be mistaken for the failed target.

## Implemented behavior

### Explicit selection contract

`CadHandleService.Select(...)` now resolves current live ObjectIds and always calls `Editor.SetImpliedSelection(...)`, including with an empty resolved set. `SelectIfAny(...)` intentionally preserves its previous no-op-on-empty behavior. The service change was merged as `e9df086f6a4cdcb66edd8fa0d7a12717e4bf5308`; the first regression gate followed as `ad52d6b2a27ab102cc87f20711a031d975665440`.

### Quantity Summary zero-candidate path

`QuantitySummaryWindow.LocateCurrent()` retains the positive-handle partial-selection/zoom flow. When the revalidated row has zero `SourceHandles`, it now calls `Cad.CadHandleService.Select(_document, liveHandles)` before invoking the existing fallback callback. Because that branch carries an empty handle set, any previous implied selection is cleared before fallback can return without a target.

### Quantity Insight zero-candidate path

`QuantityInsightPanel.LocateSelected()` now calls `Cad.CadHandleService.Select(document, handles)` inside the `handles.Count == 0` branch before reporting the non-locatable status and returning. Its normal positive-candidate selection and positive-count-only `QS3DZOOMSELECTED` behavior are unchanged.

### Focused regression gate

`scripts/preflight-quantity-locate-stale-selection-clear.py` now guards both zero-live-ID and zero-candidate cases, keeps `SelectIfAny(...)` preserve-on-empty semantics, keeps multi-object/stale-handle resolution intact, requires both quantity surfaces to clear zero-candidate selection before return/fallback, and keeps zoom behind a positive selection.

## Coordination outcome

`Commands.cs` was deliberately not modified because it was under high concurrent churn. Clearing in `QuantitySummaryWindow` before its existing callback closes the stale-highlight path without reopening that shared command surface. Reporting math/grouping, semantic identity, persistence, Excel behavior and viewport camera algorithms were not changed.

## Verification evidence

- Claim expansion was committed before follow-up source edits: `64a78875be70a93091f4a4dcdd2446c219b68ab3`.
- Follow-up planning expansion was committed before source edits: `3137c534462e9c2b8781e329300b1d9c9a55f225`.
- Branch source commits: `4ba20d17898edc6647c799544562e12e23f33f2e` (Summary zero-handle clear), `c2c312fa59d46cff31b1fb5de630142c68af392b` (Insight zero-handle clear), `96c93e028a47367f98d701a0c093199c467ab71d` (gate extension).
- PR #506 merged server-side as `8f25c5223f298baa94673604a0f6dffb39f03187` after GitHub reported the PR clean/rebaseable. High-frequency main updates prevented rebase/fast-forward attempts, so the final merge used GitHub's atomic merge operation with the expected head SHA; no force-push was used.
- Merge-SHA source blobs: Summary `2c715b976280dd1aff9aa503e187500368956206`; Insight `1bfa9f2bc0c398752687a9039f17d78949f3e10a`; focused gate `717c8712cc904c8afd713236792d4c992680d4c8`; `CadHandleService` `11c0ef8ceae29d3bb1f8870e7917fada90176eb9`.
- Re-fetched merge-SHA source confirms Summary clears before fallback, Insight clears before zero-handle return, explicit `Select` is unconditional, and `SelectIfAny` retains its early return.
- Focused gate AST parse: PASS, 162 lines. Independent ordering/source-contract checks against the re-fetched method bodies: PASS.
- Ten commits landed after merge during qualification; comparison from `8f25c5223f298baa94673604a0f6dffb39f03187` to then-current main `521e07a5f670daa2e3fd59b936c3ad29a52a59dc` showed none modifying `CadHandleService.cs`, either quantity locate file, or the focused gate.
- GitHub registered no combined status checks and no workflow runs for the merge SHA; this absence is recorded rather than treated as CI PASS.

## Native V25 disposition

Licensed interactive proof remains `PENDING_LOCAL / DO_NOT_RETRY_REMOTE` under the existing `docs/LOCAL-AGENT-INBOX.md` exact-V25/modeless/BQ qualification matrix. No duplicate local item was created.

## Completion criteria

Source-side criteria are satisfied: both zero-live-ID and zero-candidate quantity locate outcomes replace/clear the previous implied selection, partial and multi-object behavior remains intact, `SelectIfAny(...)` preserves its original contract, zoom remains positive-count-only, and focused source-level regression coverage is present.
