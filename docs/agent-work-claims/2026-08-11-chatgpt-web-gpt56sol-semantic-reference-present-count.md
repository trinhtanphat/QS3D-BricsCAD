# Work claim — semantic selection reference present-count integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-semantic-reference-present-count`
- Registered: `2026-08-11T22:29:00+07:00`
- Baseline main SHA: `12524e100f54fb46b0875598eb27200363d78b20`
- Priority: make multi-selection reference summaries report actual Family/Floor/Zone presence instead of selection size.

## Confirmed defect

`SemanticSelectionInspector.InspectReference(...)` normalizes reference IDs and correctly treats assigned-vs-unassigned values as mixed, but always constructs `SemanticSelectionTextValue` with `PresentCount = values.Count`. Blank/unassigned references are therefore counted as present.

The Workspace consumes that public summary directly in `AddMultiReferenceRow(...)` and renders mixed reference labels as `Nhiều giá trị (PresentCount/selectionCount)`. A two-element selection with one Zone and one unassigned Zone currently reports `2/2` even though only one element has a Zone reference.

Properties and quantities already define `PresentCount` as the number of selected elements that actually contain the value. Reference summaries should preserve the same contract.

## Reserved scope

- `src/QS3D.Core/Selection/SemanticSelectionInspector.cs`
- `tests/QS3D.Core.SmokeTests/SemanticSelectionInspectorSmoke.cs`
- this claim file

## Intended contract

- `Family`, `Floor` and `Zone` `PresentCount` equals the number of normalized nonblank references.
- Assigned-vs-unassigned remains `IsMixed = true` and `Value = null`.
- All-unassigned remains a non-mixed empty reference value but reports `PresentCount = 0`.
- Existing reference validation, effective-property behavior, quantity summaries and selection identity ordering stay unchanged.

## Exclusions

- No Workspace/V25 source edits; it already consumes `PresentCount`.
- No bulk-edit behavior changes.
- No selection/PICKFIRST/native lifecycle changes.
- No GitHub Actions dispatch.
- No shared smoke registry edit; `SemanticSelectionInspectorSmoke` is already registered.

## Validation plan

- Add a focused regression with one assigned and one unassigned Zone and assert `PresentCount == 1`, mixed state, and null common value.
- Add an all-unassigned reference check with `PresentCount == 0` while preserving non-mixed semantics.
- Re-fetch reserved blobs immediately before writes; stale writes must fail rather than overwrite concurrent work.

## Completion condition

Reference presence counts reflect actual nonblank assignments, focused regression is merged, exact diffs are reviewed, and this claim is closed with truthful source-only validation notes.
