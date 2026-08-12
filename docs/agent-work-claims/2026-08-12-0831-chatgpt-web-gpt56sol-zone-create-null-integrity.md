# Work claim — Zone Create null-collection integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-zone-create-null-integrity-20260812-0831`
- Registered: `2026-08-12T08:31:00+07:00`
- Baseline main SHA: `4a18251c9ae114e3897585bd5533f81719cd5eb9`
- Priority: evidence-driven Domain mutation integrity during owner-requested `continue all`

## Confirmed defect

`ProjectZoneService.Create(...)` checks `project.Zones.Any(x => x.Id ...)` and then `EnsureUniqueName(...)`, both dereferencing entries before validating the persisted Zone collection. If a malformed project contains a null Zone entry, Create therefore throws an incidental `NullReferenceException` rather than failing closed with the domain integrity contract before mutation. The sibling `ProjectFloorService.Create(...)` has just gained an explicit null-collection preflight on current `main`, and `ProjectState.FindZone(...)` already treats null Zone entries as invalid project state.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectZoneService.cs` — `Create(...)` preflight only.
- `tests/QS3D.Core.SmokeTests/ProjectZoneCreateNullIntegritySmoke.cs` — focused CAD-independent regression.
- this claim file.

## Contract

Reject a null entry in `project.Zones` before max-count, duplicate-id/name checks, `project.Touch()` or collection mutation. Preserve all canonical active-zone, assignment, update/delete/reference-count behavior and existing Zone limits/validation.

## Coordination

The prior Floor/Zone corrective claim is `COMPLETED`; this lane does not reopen active-id semantics or existing `ProjectZoneServiceSmoke.cs`. No Family/Floor service, UI, persistence or native CAD files are reserved.

## Validation plan

Prove malformed null-Zone state throws `InvalidOperationException`, leaves ChangeVersion and Zone count unchanged, and ordinary Zone Create still adds the requested Zone and advances state normally. Re-fetch source before write; never force-push. No GitHub Actions dispatch or BricsCAD runtime qualification claim.
