# Work claim — Quantity Rule Preview global Element identity integrity

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-quantity-preview-global-element-integrity-20260812-0904`
- Registered: `2026-08-12T09:04:00+07:00`
- Completed: `2026-08-12T09:06:00+07:00`
- Baseline main SHA: `bd5a2bd242ddc924fd68c84867492e96d0e96ccd`
- Claim commit: `cd733ffa853ed961fc37b512850216966e7ecd5f`
- Source fix commit: `ddc233073ac887efd50007aef115faa7ce4b18ef`
- Focused smoke commit: `24bd9b58634207a6b6de33bc9de2fed490805f04`
- Priority: P1 — quantity preview must not produce reviewable output from globally ambiguous Element identity state.
- Task Key: `CORE-QUANTITY-PREVIEW-GLOBAL-DUPLICATE-ELEMENT-ID`

## Confirmed defect

`QuantityRulePreviewService.RequireOwnedElement(...)` resolved only the requested target through `ProjectState.FindElement(targetId)`, while `PreviewProject(...)` detached and enumerated project Elements without global ID validation. An unrelated `E1`/`e1` pair plus unique `E2` could therefore produce normal Element/Project previews from globally invalid semantic identity state.

## Implemented contract

- `ValidateUniqueElementIds(...)` scans all non-null project Elements case-insensitively and rejects duplicate IDs with `Project contains duplicate element id: <id>`.
- `RequireOwnedElement(...)` invokes that preflight before exact-instance target resolution, covering Element preview/apply entry paths.
- `PreviewProject(...)` invokes the same preflight before detached-copy preview generation; project apply freshness flows reuse PreviewProject.
- Existing null-entry behavior, exact-instance ownership, preview freshness, QuantityRuleEngine semantics and health guard behavior remain unchanged.
- ProjectStateSnapshot, persistence, UI and native BricsCAD code were not modified.

## Validation evidence

- Current `main` readback confirms project-wide and exact-owned preview paths invoke global Element-ID validation.
- `QuantityRulePreviewGlobalElementIntegritySmoke` is auto-registered and proves `PreviewElement(E2)` and `PreviewProject()` reject `E1`/`e1 + E2` with the canonical duplicate error.
- The same smoke preserves deterministic zero-change Element/Project preview semantics for canonical unique Elements.
- This connector-only session did not execute .NET smoke, GitHub Actions or licensed BricsCAD runtime tests.

## Completion

`COMPLETED`: Quantity Rule Preview no longer produces reviewable output from projects with unrelated duplicate Element identities.
