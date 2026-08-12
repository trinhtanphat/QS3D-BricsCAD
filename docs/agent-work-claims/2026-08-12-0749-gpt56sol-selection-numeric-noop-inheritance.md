# Work claim — selection numeric no-op inheritance preservation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-selection-numeric-noop-inheritance-20260812-0749`
- Registered: `2026-08-12T07:49:00+07:00`
- Baseline main SHA: `117f529eaf88b8b30ddc8a788e849924915f0eb6`
- Priority: evidence-driven remote-safe semantic mutation integrity during owner-requested `continue all`

## Confirmed defect

`SemanticSelectionBulkEditService.MultiplyNumericProperty()` determines no-op state by comparing the original property text with `next.ToString("R", InvariantCulture)`. Numerically identical values with different lexical forms therefore mutate. For an inherited Family property such as `"1.0"`, multiplying by `1` writes an instance override `"1"`, changes project revision state, and breaks future Family inheritance even though the numeric value did not change.

## Reserved scope

Preserve true numeric no-ops in `MultiplyNumericProperty()` based on numeric equality rather than string-format equality. A numerically unchanged effective value must not rewrite an existing instance value and must not materialize a new instance override for an inherited Family value.

## Expected surfaces

- `src/QS3D.Core/Selection/SemanticSelectionBulkEditService.cs`
- focused Core smoke coverage under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Excluded scope

- `ProjectQuantityReportBuilder` selection canonicality, currently reserved by the Goal-100 claim.
- `SemanticSelectionInspector`, bulk Family assignment, generic `SetProperty`, property-key policy, UI/native selection, quantity reporting, or BricsCAD runtime behavior.
- No numeric tolerance policy: only exact `double` result equality is treated as a no-op.
- No GitHub Actions or LOCAL_ONLY qualification.

## Validation plan

- Inherited Family property `"1.0"` multiplied by `1` reports zero changes, preserves `ChangeVersion`, and does not create an instance property.
- Existing instance property `"1.0"` multiplied by `1` reports zero changes, preserves raw text and `ChangeVersion`.
- A real multiplication such as ×2 still reports one change and writes the expected instance value.
- Existing non-finite/parse/overflow behavior remains unchanged.
- Re-read exact branch diff and moving `main` before integration; do not dispatch Actions.

## Coordination

The active Goal-100 quantity-selection claim is confined to `ProjectQuantityReportBuilder.cs`, its smoke/registration and a planning document. Recent history contains an older atomic numeric bulk-edit fix and a distinct bulk Family-assignment no-op fix, but no numeric no-op/inheritance lane for this service.

## Completion condition

Focused source and regression are merged to current `main`, remote source is re-read, and this claim is marked `COMPLETED` with exact integration SHA and validation boundaries.
