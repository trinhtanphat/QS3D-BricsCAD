# Work claim — Quantity Rule Preview global Element identity integrity

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-preview-global-element-integrity-20260812-0904`
- Registered: `2026-08-12T09:04:00+07:00`
- Baseline main SHA: `bd5a2bd242ddc924fd68c84867492e96d0e96ccd`
- Priority: P1 — quantity preview must not produce reviewable output from globally ambiguous Element identity state.
- Task Key: `CORE-QUANTITY-PREVIEW-GLOBAL-DUPLICATE-ELEMENT-ID`

## Confirmed defect

`QuantityRulePreviewService.RequireOwnedElement(...)` calls `ProjectState.FindElement(targetId)`, which detects duplicate IDs only when they match the requested target. `PreviewProject(...)` creates a detached project copy and enumerates its Elements without first validating global Element identity. A malformed project containing unrelated `E1`/`e1` plus unique target `E2` can therefore produce a normal Element or Project quantity-rule preview for `E2` even though QSDB and DependencyGraph reject the project globally. Apply flows may only fail later when snapshot/other integrity boundaries execute.

## Reserved scope

- `src/QS3D.Core/Rules/QuantityRulePreviewService.cs`
- `tests/QS3D.Core.SmokeTests/QuantityRulePreviewGlobalElementIntegritySmoke.cs`
- this claim file

## Intended contract

- Quantity-rule preview/apply ownership preflight checks all non-null project Element IDs case-insensitively for uniqueness before exact target resolution.
- Project-wide preview performs the same global Element-ID preflight before detached-copy preview generation.
- Unrelated duplicate IDs fail closed with the canonical `Project contains duplicate element id: <id>` error.
- Existing null-entry behavior, exact-instance ownership, preview freshness, quantity-rule semantics and health guard behavior remain unchanged.
- No changes to ProjectStateSnapshot, QuantityRuleEngine, persistence, UI or native BricsCAD code.

## Validation plan

Focused auto-registered Core smoke seeds `E1`/`e1` plus unique `E2`, verifies `PreviewElement(E2)` and `PreviewProject()` reject, and proves canonical project preview remains stable. Re-fetch exact source/claim before writes. No force-push, Actions dispatch, .NET smoke PASS or licensed BricsCAD runtime qualification claim unless actually executed.
