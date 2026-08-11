# Work claim — Semantic selection inspector physical-opening ownership filtering

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-selection-inspector-physical-opening-ownership`
- Registered: `2026-08-12T00:42:00+07:00`
- Baseline main SHA: `4fce6a653e5438fe21bb18a8841b6d619284f0d5`
- Priority: P1 — keep drawing-local physical-opening ownership metadata out of semantic property inspection.

## Confirmed defect

`SemanticSelectionInspector.IsInternalOwnershipProperty(...)` hides handle-bearing, `QS3D.Generated...` and legacy `PhysicalOpeningCut...` keys, but does not hide the actual namespaced physical-opening state used by `PhysicalOpeningCutTargetStateCodec`: `QS3D.PhysicalOpeningCutOpeningIds`. As a result, effective selection properties can surface internal native/drawing-local cut ownership metadata in the semantic Workspace inspector.

A separate active lane is hardening `SemanticPropertyEditPolicy` for the same namespace at the generic edit boundary. This claim is deliberately disjoint: it reserves only the read-only selection inspector and its focused smoke/static gate so internal ownership does not appear as a user semantic property in the first place.

## Reserved scope

- `src/QS3D.Core/Selection/SemanticSelectionInspector.cs`
- `tests/QS3D.Core.SmokeTests/SemanticSelectionInspectorSmoke.cs`
- `scripts/preflight-selection-inspector-physical-opening-ownership.py` (new)
- this claim file for close-out

## Intended contract

- Both legacy `PhysicalOpeningCut...` and namespaced `QS3D.PhysicalOpeningCut...` keys are filtered from Family/Element effective property inspection.
- Ordinary semantic properties remain visible.
- Handle/generated filtering and all selection/reference fail-closed behavior remain unchanged.
- Focused smoke includes the real namespaced opening-target ownership key shape.

## Excluded scope

No `SemanticPropertyEditPolicy` changes, no target-state codec/boolean/native changes, no Workspace WPF changes, no Actions dispatch and no V25 runtime claim.

## Completion condition

Namespaced physical-opening ownership is absent from semantic selection inspection, focused smoke/static coverage is on current `main`, and this claim is closed with exact SHAs and truthful validation boundaries.
