# Work claim — Curtain schedule valid delimiter-collision fixture

- Status: `ACTIVE`
- Agent: `codex-curtain-schedule-valid-collision-fixture-20260814` (`/root/fix_level_curtain_frame_z`, delegated by `/root`)
- Registered: `2026-08-14T13:53:00+07:00`
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
