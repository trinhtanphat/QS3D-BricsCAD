# Work claim — Material Usage schedule valid delimiter-collision fixture

- Status: `ACTIVE`
- Agent: `codex-material-usage-valid-collision-fixture-20260814` (`/root/fix_level_curtain_frame_z`, delegated by `/root`)
- Registered: `2026-08-14T13:35:45+07:00`
- Baseline main SHA: `de2d032f62caacd9583fa4e61db7dcf2d39c5523`
- Priority: continue the independent Core smoke fixture reconciliation after the completed Door collision fix

## Diagnosis

`MaterialUsageScheduleGroupKeyCollisionSmoke` constructs a `FloorDefinition` whose ID contains U+001F. Current `FloorDefinition` correctly rejects control characters, so module initialization fails before the fixture reaches its grouping assertions. `MaterialUsageScheduleBuilder` already uses length-prefixed Floor/Material/Component/Category/Family tokens; this is stale fixture data, not a reporting or identity-policy defect.

## Reserved scope

- `tests/QS3D.Core.SmokeTests/MaterialUsageScheduleGroupKeyCollisionSmoke.cs`
- this claim file
- parent LOCAL-003 claim only for the explicit delegation/completion record

Use a valid printable delimiter in the Floor IDs and Material text. Add an explicit test-local assertion proving the two distinct five-token tuples collide under delimiter-only legacy serialization, then retain the existing production assertions that the schedule returns two rows while the identical tuple aggregates count and length.

## Excluded scope

No production reporting/domain change, no adjacent schedule fixture, and no Level production, probe, runner, BricsCAD, private data, GitHub Actions, V26, release or packaging change.

## Validation and completion

Run the strict Core smoke Release build, registered full Core smoke, and relevant Material Usage schedule gates. If the complete smoke reaches a separate stale fixture, report it without expanding this claim. Merge the test-only correction through a normal PR, record exact SHAs, then mark this claim `COMPLETED`.
