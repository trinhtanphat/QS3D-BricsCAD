# Work claim — Curtain wall schedule structural read-only result

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-curtain-schedule-readonly-result-20260812-0816`
- Registered: `2026-08-12T08:16:00+07:00`
- Baseline main SHA: `8462d9577de021e56d028f040a3a04264636e317`
- Priority: evidence-driven Reporting result ownership during owner-requested `continue all`

## Confirmed defect

`CurtainWallScheduleBuilder.Build(ProjectState)` declares `IReadOnlyList<CurtainWallScheduleRow>` but returns `order.Select(...).ToList()` directly. Callers can cast the returned value to a mutable collection and structurally add, remove or clear rows after schedule aggregation is complete. The neighboring Door/Opening schedule already wraps its completed list in `AsReadOnly()`.

## Reserved scope

- `src/QS3D.Core/Reporting/CurtainWallSchedule.cs` — return boundary only.
- `tests/QS3D.Core.SmokeTests/CurtainWallScheduleReadOnlyResultSmoke.cs` — focused CAD-independent regression.
- this claim file.

## Contract

Return a structural read-only wrapper for the completed curtain schedule rows while preserving ordering, grouping keys, wall/panel/frame quantities, min/max values, project/drawing identity, provenance, and all validation/overflow behavior. No deep-immutability redesign of `CurtainWallScheduleRow`.

## Excluded scope

No curtain generation/regeneration, frame geometry, ownership metadata, UI/modeless lifetime, XLSX export, CAD/native behavior, persistence or release/update changes.

## Validation plan

Build a minimal valid curtain schedule, preserve ordinary row/count semantics, require `ICollection<CurtainWallScheduleRow>.IsReadOnly`, and prove structural `Add` throws `NotSupportedException`. Re-fetch source before write; never force-push. No GitHub Actions dispatch or BricsCAD runtime qualification claim.
