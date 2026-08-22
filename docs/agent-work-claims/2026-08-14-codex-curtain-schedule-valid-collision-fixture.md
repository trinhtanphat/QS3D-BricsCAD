# Work claim — Curtain schedule valid delimiter-collision fixture

- Status: `COMPLETED`
- Agent: `codex-curtain-schedule-valid-collision-fixture-20260814` (`/root/fix_level_curtain_frame_z`, delegated by `/root`)
- Registered: `2026-08-14T13:53:00+07:00`
- Completed: `2026-08-14T14:28:00+07:00`
- Baseline main SHA: `80248ec69db8ee69a7b21e73dd6fa6f37b068368`
- Priority: unblock complete Core smoke during LOCAL-003 diagnostic validation without changing production

## Diagnosis

`CurtainWallScheduleGroupKeyCollisionSmoke` constructs Floor and GlassWall Family definitions whose IDs and names contain U+001F. Current `FloorDefinition` and `ProjectFamily` correctly reject control characters, so module initialization now fails before the fixture reaches its grouping assertions. `CurtainWallScheduleBuilder` already uses length-prefixed Floor/Family tokens; this is stale fixture data, not a reporting or identity-policy defect.

## Reserved scope

- `tests/QS3D.Core.SmokeTests/CurtainWallScheduleGroupKeyCollisionSmoke.cs`
- this claim file
- parent LOCAL-003 claim only for the explicit delegation/completion record

Reconcile the fixture with valid printable-delimiter Floor/Family IDs and names. Add an explicit test-local assertion proving the two distinct tuples collide under a delimiter-only legacy serializer, then retain the existing production assertions that the length-prefixed schedule key returns two rows while identical tuples aggregate.

## Excluded scope

No production reporting/domain change, no adjacent schedule fixture, and no Level production, probe, runner, runtime documentation, BricsCAD, private data, GitHub Actions, V26, release or packaging change.

## Validation and completion

Run the focused Curtain collision smoke through the Core smoke executable, the complete Core smoke, and the relevant Curtain schedule gate. If the complete smoke reaches a separate stale fixture, report it without expanding this claim. Merge the test-only correction through a normal PR, record exact SHAs, then mark this claim `COMPLETED`.

## Completion record

- Claim-only PR `#1162` merged as `b7abc40be6e0b98f6a53e8ccab161c6265b44faa` before the test edit.
- Implementation source commit `a35a7a6a7055692fa5777c66370d8f2927c8a909` merged through PR `#1164` as `8d33c39f9cbd30cf37606c67dba44244cfb843b3`.
- The fixture now uses printable `|` Floor/Family identities, explicitly proves the two tuples collide under delimiter-only legacy serialization, and retains all production grouping/count/quantity/name assertions.
- Core smoke Release build passed with zero warnings/errors. The Curtain wall UI/export and read-only schedule project-safety gates passed. The complete registered smoke advanced past this Curtain fixture and then stopped at the independent `DoorOpeningScheduleGroupKeyCollisionSmoke` control-character Family fixture; Door remains unchanged and outside this claim.
- No production, domain, Level, probe/runner, BricsCAD, private-data or GitHub Actions surface was changed or executed.
