# Work claim — Room finish generator structural read-only result

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-room-finish-generator-readonly-result-20260812-0843`
- Registered: `2026-08-12T08:43:00+07:00`
- Baseline main SHA: `0970f1cb7779bcd95d2617c80e66dabb341c1b2a`
- Priority: evidence-driven public result ownership during owner-requested `continue all`

## Confirmed defect

`RoomFinishGenerator.Generate(...)` declares `IReadOnlyList<ElementInstance>` but returns its mutable backing `List<ElementInstance>` directly. Callers can cast the generated preview/result collection to a mutable collection and structurally add, remove or clear generated finish instances after generation has completed.

## Reserved scope

- `src/QS3D.Core/Services/RoomFinishGenerator.cs` — final return boundary only.
- `tests/QS3D.Core.SmokeTests/RoomFinishGeneratorReadOnlyResultSmoke.cs` — focused CAD-independent regression.
- this claim file.

## Contract

Return a structural read-only wrapper for the generated finish list while preserving Room-category validation, numeric finite/non-negative checks, enabled finish categories, IDs/families/floor/material/source-handle propagation and generated metric values. No deep-immutability redesign of `ElementInstance`.

## Coordination

The previous room-finish-generator numeric-safety claim is `COMPLETED`. This lane does not edit Room Finish synchronization/health, Auto Room lifecycle, native generation, UI or existing numeric smoke files.

## Validation plan

Generate representative Floor Finish + Skirting outputs, preserve order/category/metric/source-handle semantics, require returned `ICollection<ElementInstance>` to be read-only, and prove structural `Add` throws `NotSupportedException`. Re-fetch source before write; never force-push. No GitHub Actions dispatch or BricsCAD runtime qualification claim.
