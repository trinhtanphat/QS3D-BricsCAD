# Work claim — Curtain Panel generated area health

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-curtain-panel-area-health-20260812-1151`
- Registered: `2026-08-12T11:51:00+07:00`
- Baseline main SHA: `4c02e8742ee40b99afacc465fe80778552a38d67`
- Priority: P1 generated-output health completeness

## Confirmed defect

Both production Curtain Panel writers persist `GeneratedCurtainPanelAreaM2` from the bounded panel plan using exact `double.ToString("R", CultureInfo.InvariantCulture)`. `GeneratedCurtainPanelHealthService.Inspect(...)` currently validates panel depth, source length and height but never validates the persisted area snapshot. Missing, malformed, NaN/Infinity, negative, or writer-noncanonical area metadata can therefore remain health-clean.

Zero area is valid because a successful opening-aware panel build can legitimately produce zero remaining panel area/pieces, so this field must be finite and non-negative rather than strictly positive.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedCurtainPanelHealthService.cs`, only generated area validation
- one focused auto-registered smoke under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Intended contract

- Require `GeneratedCurtainPanelAreaM2` to exist, parse invariantly, remain finite, and be `>= 0`.
- Missing/malformed/NaN/Infinity/negative values emit `CURTAIN_PANEL_AREA_INVALID` as a Warning.
- Otherwise require exact ordinal equality with `value.ToString("R", CultureInfo.InvariantCulture)`; writer aliases such as padded text or explicit plus emit `CURTAIN_PANEL_AREA_NON_CANONICAL` as an Error.
- Preserve zero-area builds and all existing handles/counts/build-state/fingerprint/mode/stale/native semantics.
- Do not rewrite persisted metadata during health inspection.

## Validation boundary

Add focused Core smoke coverage for canonical positive/zero area, missing/invalid/negative area, and noncanonical aliases. Source/readback validation only; no GitHub Actions/full build/executable smoke or licensed BricsCAD V25/V26 runtime PASS is claimed without execution.
