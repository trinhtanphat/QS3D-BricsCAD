# Work claim — semantic selection reference present-count integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-semantic-reference-present-count`
- Registered: `2026-08-11T22:29:00+07:00`
- Completed: `2026-08-11T22:32:00+07:00`
- Baseline main SHA: `12524e100f54fb46b0875598eb27200363d78b20`
- Reservation commit: `cf3f9fc94744da5fa5bf1704c43c42314cb72080`
- Priority: make multi-selection reference summaries report actual Family/Floor/Zone presence instead of selection size.

## Defect fixed

`SemanticSelectionInspector.InspectReference(...)` previously normalized reference IDs and detected assigned-vs-unassigned mixing correctly, but always reported `PresentCount = values.Count`. Blank/unassigned Family/Floor/Zone references were therefore counted as present.

The inspector now counts only normalized nonblank references when constructing `SemanticSelectionTextValue`. This matches the existing `PresentCount` contract for properties and quantities and makes Workspace `x/n` mixed-reference labels truthful without modifying V25 UI code.

## Reserved scope

- `src/QS3D.Core/Selection/SemanticSelectionInspector.cs`
- `tests/QS3D.Core.SmokeTests/SemanticSelectionInspectorSmoke.cs`
- this claim file

## Delivered contract

- `Family`, `Floor` and `Zone` `PresentCount` equals the number of normalized nonblank references.
- Assigned-vs-unassigned remains `IsMixed = true` and `Value = null`.
- All-unassigned remains a non-mixed empty reference value and now reports `PresentCount = 0`.
- Existing reference validation, effective-property behavior, quantity summaries and selection identity ordering remain unchanged.

## Published commits

- `b69b690fea4f3f09fe10c9f7ef9b5ff31a92dd19` — count actual nonblank semantic references in `InspectReference(...)`.
- `5cbf9fb0861a3880b0e528c066cca68c692dddef` — cover partial (`1/2`) and all-unassigned (`0/2`) Zone reference presence.

## Validation notes

- Exact source diff only introduces the nonblank `present` count and passes it to the existing summary object.
- Exact smoke diff only registers and implements the focused reference-presence regression inside the already-registered inspector smoke suite.
- Workspace was read to verify the consumer renders `summary.PresentCount/selectionCount`; no Workspace/V25 source was modified.
- The current execution environment has no `dotnet`, so executable smoke/build evidence was not produced in this session.
- GitHub Actions were not dispatched and no force-push was used.

## Exclusions

- No Workspace/V25 source edits.
- No bulk-edit behavior changes.
- No PICKFIRST/native lifecycle changes.

## Completion condition

Satisfied for the source/static contract. Executable Core/V25 qualification remains a separate exact-SHA gate.
