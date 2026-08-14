# Work claim — Project Quantity valid delimiter-collision fixture

- Status: `ACTIVE`
- Agent: `codex-project-quantity-valid-collision-fixture-20260814` (`/root/fix_level_curtain_frame_z`, delegated by `/root`)
- Registered: `2026-08-14T14:05:53+07:00`
- Baseline main SHA: `15a48b5898e932302b93af39df9a17accb9f9f80`
- Priority: continue the first observable Core smoke blocker after Browser workspace reconciliation

## Diagnosis

`ProjectQuantityReportGroupKeySmoke.DelimiterInjectionDoesNotMergeDistinctRows` constructs a `ProjectFamily` whose ID contains U+001F. Current `ProjectFamily` correctly rejects control characters, so module initialization fails before the fixture reaches its grouping assertions. `ProjectQuantityReportBuilder` already uses length-prefixed Floor/Zone/Category/Family/Material/Density tokens; this is stale fixture data, not a reporting or identity-policy defect.

## Reserved scope

- `tests/QS3D.Core.SmokeTests/ProjectQuantityReportGroupKeySmoke.cs`
- this claim file
- parent LOCAL-003 claim only for the explicit delegation/completion record

Use printable `|` in the Family IDs and Material text. Add an explicit test-local assertion proving the two distinct six-token tuples collide under delimiter-only legacy serialization, then retain the existing two-row Family/Material/count/length separation checks and the legitimate one-row count-two/length-five aggregation case.

## Excluded scope

No production reporting/domain change, no runner/module-initializer architecture or adjacent fixture, and no Level production, probe, runner, BricsCAD, private data, GitHub Actions, V26, release or packaging change.

## Validation and completion

Run the strict Core smoke Release build, registered full Core smoke, and focused Project Quantity/reporting gates. If the complete smoke reaches a separate stale fixture, report it without expanding this claim. Merge the test-only correction through a normal PR, record exact SHAs, then mark this claim `COMPLETED`.
