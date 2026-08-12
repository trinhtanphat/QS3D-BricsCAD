# Work claim — Grid renumber structural read-only result

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-grid-renumber-readonly-result-20260812-0838`
- Registered: `2026-08-12T08:38:00+07:00`
- Baseline main SHA: `7805ab5d978147ce01f083dc98f4393e9537af04`
- Priority: evidence-driven public result ownership during owner-requested `continue all`

## Confirmed defect

`GridNamingService.Renumber(...)` declares `IReadOnlyList<GridLabelAssignment>` but returns its mutable backing `List<GridLabelAssignment>` directly. A caller can cast the returned plan to `ICollection<GridLabelAssignment>` and structurally add, remove or clear assignments after the renumber result has been published.

## Reserved scope

- `src/QS3D.Core/Domain/GridNamingService.cs` — final return boundary only.
- `tests/QS3D.Core.SmokeTests/GridRenumberReadOnlyResultSmoke.cs` — focused CAD-independent regression.
- this claim file.

## Contract

Return a structural read-only wrapper for the completed renumber assignment plan while preserving input cap, sequence/label validation, target resolution, reserved-label collision checks, canonical no-op behavior, project Touch semantics and element property mutations. `GridLabelAssignment` remains immutable as-is.

## Coordination

Recent Grid naming bounded-enumeration, reserved-label-integrity and null-health claims are all `COMPLETED`. This lane does not edit health providers, Grid annotation/native generation, command lifecycle or existing smoke/preflight files.

## Validation plan

Renumber two ordinary Grid elements, preserve assignment order/labels and semantic properties, require the returned `ICollection<GridLabelAssignment>` to be read-only, and prove structural `Add` throws `NotSupportedException`. Re-fetch source before write; never force-push. No GitHub Actions dispatch or BricsCAD runtime qualification claim.
