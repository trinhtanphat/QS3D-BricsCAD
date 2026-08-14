# Work claim — Door schedule valid delimiter-collision fixture

- Status: `ACTIVE`
- Agent: `codex-door-schedule-valid-collision-fixture-20260814` (`/root/fix_level_curtain_frame_z`, delegated by `/root`)
- Registered: `2026-08-14T13:29:53+07:00`
- Baseline main SHA: `8bad1dc3430230279f54dd03d181b456789ab1a4`
- Priority: continue the independent Core smoke fixture reconciliation after the completed Curtain collision fix

## Diagnosis

`DoorOpeningScheduleGroupKeyCollisionSmoke` constructs a Door `ProjectFamily` whose ID and name contain U+001F. Current `ProjectFamily` correctly rejects control characters, so module initialization fails before the fixture reaches its grouping assertions. `DoorOpeningScheduleBuilder` already uses length-prefixed Floor/Category/Family/numeric/Material tokens; this is stale fixture data, not a reporting or identity-policy defect.

## Reserved scope

- `tests/QS3D.Core.SmokeTests/DoorOpeningScheduleGroupKeyCollisionSmoke.cs`
- this claim file
- parent LOCAL-003 claim only for the explicit delegation/completion record

Use valid printable-delimiter Family IDs/names and Material text. Add an explicit test-local assertion proving the two distinct eight-token tuples collide under delimiter-only legacy serialization, then retain the existing production assertions that the schedule returns two rows while the identical tuple aggregates count and opening area.

## Excluded scope

No production reporting/domain change, no adjacent schedule fixture, and no Level production, probe, runner, BricsCAD, private data, GitHub Actions, V26, release or packaging change.

## Validation and completion

Run the strict Core smoke Release build, registered full Core smoke, and relevant Door schedule gates. If the complete smoke reaches a separate stale fixture, report it without expanding this claim. Merge the test-only correction through a normal PR, record exact SHAs, then mark this claim `COMPLETED`.
