# Work claim — Curtain Panel fingerprint canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-curtain-panel-fingerprint-canonicality-20260812-1125`
- Registered: `2026-08-12T11:25:00+07:00`
- Priority: P1 generated-output health parity

## Confirmed defect

`CurtainWallPanelFingerprint.Compute(...)` emits a 64-character lowercase SHA-256 digest using `x2`. `GeneratedCurtainPanelHealthService.Fingerprint(...)` currently trims the persisted snapshot and only checks length/hex shape, so writer-noncanonical aliases such as uppercase or surrounding whitespace remain health-clean. The sibling Curtain Frame health provider already makes equivalent writer-owned fingerprint aliases fail-visible.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedCurtainPanelHealthService.cs`
- one focused auto-registered Core smoke under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Intended contract

Keep existing missing/invalid fingerprint warning semantics. For an otherwise valid 64-hex digest, require the stored text itself to equal its lowercase spelling ordinally and contain no surrounding whitespace. Emit `CURTAIN_PANEL_CONFIG_FINGERPRINT_NON_CANONICAL` as `HealthSeverity.Error` for aliases. Do not alter fingerprint computation, integer/handle/mode/build-state/floating metadata, stale logic, native materialization, or BricsCAD runtime behavior.

## Validation boundary

Add focused regression coverage for uppercase, padded and exact-lowercase snapshots plus invalid-shape precedence. Source-safe readback only; no GitHub Actions/full build/executable smoke or BricsCAD V25/V26 runtime PASS claimed without execution.
