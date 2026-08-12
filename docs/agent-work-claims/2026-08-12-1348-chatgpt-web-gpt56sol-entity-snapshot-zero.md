# Work claim — EntitySnapshot canonical metric zero

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-entity-snapshot-zero-20260812-1348`
- Registered: `2026-08-12T13:48:00+07:00`
- Completed: `2026-08-12T13:51:00+07:00`
- Baseline main SHA: `97b78b2c9fa4114cd3cbd837e9d71aaa1a8a0b30`
- Claim commit: `67b06617aae371c4289e9298361b8434100bfe6a`
- Implementation commit: `8c4a2beedc0dec43d24937349aa92b66f3c9cde9`
- Regression commit: `54d565d5e9d33f26fd31467a1ca222733e81a896`
- Priority: P2 — non-negative snapshot metrics should have one canonical zero representation.

## Confirmed defect

`EntitySnapshot.RequireFinite(...)` rejected negative finite values with `value < 0d`, but IEEE negative zero compares equal to zero and therefore passed through unchanged. All four native metric setters could retain the negative-zero sign bit even though EntitySnapshot's established contract is finite and non-negative. The repository already canonicalizes signed zero at generated identity, grouping, fingerprint, and display boundaries to avoid two representations of the same semantic zero.

## Implemented

- Preserve null as unavailable.
- Preserve rejection of NaN, infinities, and negative finite metrics.
- Preserve positive finite metrics unchanged.
- Canonicalize every provided numeric zero to positive IEEE zero in the shared metric validator.
- Extend the existing non-negative metric smoke across length, area, surface-area, and volume with explicit negative-zero sign-bit coverage plus existing invalid-value checks.
- No Takeoff, exporter, CAD proxy capture, or measurement-unit conversion behavior was changed.

## Reserved scope

- `src/QS3D.Core/Model/EntitySnapshot.cs`
- `tests/QS3D.Core.SmokeTests/EntitySnapshotNonNegativeMetricsSmoke.cs`
- this claim file

## Validation performed

- Re-fetched `EntitySnapshot.cs` and the existing smoke after claim publication before editing and confirmed their blobs were unchanged under concurrent work.
- Re-fetched both files from `main` after publication and verified source blob `863960de98a42789a559739613e53857c5c8bbc7` and smoke blob `920b7209e7f563f313bf38262a5d9195c367851f` contain the intended patch.
- The regression constructs IEEE negative zero from `BitConverter.Int64BitsToDouble(long.MinValue)` and verifies stored zero has a zero sign bit for all four metrics.
- No GitHub Actions workflow was dispatched or re-run. No hosted/local .NET PASS or BricsCAD V25/V26 runtime PASS is claimed without execution.

## Outcome

EntitySnapshot now has one canonical zero representation for all available non-negative metrics while preserving null, positive finite values, and existing invalid-value rejection. Scope is released.
