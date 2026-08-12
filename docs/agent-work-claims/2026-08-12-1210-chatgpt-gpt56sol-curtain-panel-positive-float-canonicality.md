# Work claim — Curtain Panel positive float canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-curtain-panel-positive-float-canonicality-20260812-1210`
- Registered: `2026-08-12T12:10:00+07:00`
- Priority: P1 generated-output health parity

## Confirmed defect

Both production Curtain Panel writers persist `GeneratedCurtainPanelDepthM`, `GeneratedCurtainPanelSourceLengthM`, and `GeneratedCurtainPanelHeightM` with exact invariant round-trip (`R`) formatting. `GeneratedCurtainPanelHealthService.Positive(...)` only broad-parses these writer-owned snapshots and checks finite `> 0`, so numeric aliases such as explicit plus, padding, or trailing-zero spellings can remain health-clean.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedCurtainPanelHealthService.cs`, `Positive(...)` canonicality only
- one focused auto-registered Core smoke under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Intended contract

- Preserve existing missing/malformed/non-finite/non-positive Warning codes per field.
- After successful positive validation, require exact ordinal equality with `value.ToString("R", CultureInfo.InvariantCulture)`.
- Writer-noncanonical aliases emit Error `CURTAIN_PANEL_FLOAT_METADATA_NON_CANONICAL`, retaining the parsed value/normal health flow.
- Do not change area, sagitta, integer, handle, mode, fingerprint, stale, writer/native or persistence behavior.

## Validation boundary

Focused source/readback + Core smoke source only unless an executable build is actually run. No GitHub Actions or licensed BricsCAD runtime PASS is claimed.
