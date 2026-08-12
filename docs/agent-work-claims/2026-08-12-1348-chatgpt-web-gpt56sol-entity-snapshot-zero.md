# Work claim — EntitySnapshot canonical metric zero

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-entity-snapshot-zero-20260812-1348`
- Registered: `2026-08-12T13:48:00+07:00`
- Baseline main SHA: `97b78b2c9fa4114cd3cbd837e9d71aaa1a8a0b30`
- Priority: P2 — non-negative snapshot metrics should have one canonical zero representation.

## Confirmed defect

`EntitySnapshot.RequireFinite(...)` rejects negative finite values with `value < 0d`, but IEEE negative zero compares equal to zero and therefore passes through unchanged. All four native metric setters can retain the negative-zero sign bit even though EntitySnapshot's established contract is finite and non-negative. The repository already canonicalizes signed zero at generated identity, grouping, fingerprint, and display boundaries to avoid two representations of the same semantic zero.

## Reserved scope

- `src/QS3D.Core/Model/EntitySnapshot.cs`
- `tests/QS3D.Core.SmokeTests/EntitySnapshotNonNegativeMetricsSmoke.cs`
- this claim file

## Intended contract

- Preserve null as unavailable.
- Preserve rejection of NaN, infinities, and negative finite metrics.
- Preserve positive finite metrics unchanged.
- Canonicalize every provided numeric zero to positive IEEE zero.
- Do not modify Takeoff, exporters, CAD proxy capture, or measurement-unit conversion behavior.

## Validation boundary

Focused source/readback regression only. No GitHub Actions dispatch, hosted/local .NET PASS, or BricsCAD V25/V26 runtime PASS is claimed without execution.
