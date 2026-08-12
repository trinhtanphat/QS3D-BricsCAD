# Work claim — Foundation Mesh generated-handle canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-foundation-mesh-handle-canonicality-20260812-1035`
- Registered: `2026-08-12T10:35:00+07:00`
- Priority: P1 generated-output health parity

## Confirmed defect

`GeneratedFoundationMeshHealthService.Inspect(...)` trims every token from `GeneratedFoundationMeshHandles` before validation. Persisted writer-owned values such as `" A "` are therefore accepted as canonical `"A"` with no health evidence. Sibling generated-rebar, Tie Rebar, Beam Stirrup and Slab Mesh diagnostics now surface surrounding whitespace while continuing duplicate/ownership/source/liveness checks with the trimmed handle.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedFoundationMeshHealthService.cs`
- one focused auto-registered Core smoke under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Intended contract

Emit a dedicated `HealthSeverity.Error` when an otherwise valid hex handle token has surrounding whitespace, then continue all existing checks using the trimmed handle. Preserve lowercase hex acceptance, empty-token invalidity, duplicate detection, ownership conflicts, SourceHandles rejection, live-solid lookup, count validation, diameter/spacing/cover/faces/mode/footprint/category/stale behavior.

## Validation boundary

Source-safe focused regression + exact readback only. No GitHub Actions/full build/executable smoke or BricsCAD V25/V26 runtime PASS claimed without execution.
