# Work claim — Curtain Panel generated area health

- Status: `COMPLETED`
- Agent: `chatgpt-gpt56sol-curtain-panel-area-health-20260812-1151`
- Registered: `2026-08-12T11:51:00+07:00`
- Completed: `2026-08-12T12:05:00+07:00`
- Baseline main SHA: `4c02e8742ee40b99afacc465fe80778552a38d67`
- Claim commit: `2ef5d4cc0730ac98013a056636127cb6f1315918`
- Source fix commit: `18a654c4de0044b2f047a6962634c230e5e88934`
- Focused smoke commit: `531d5bc6f68031e4dca052a04fbb36a6e72d441f`
- Integration PR: `#857`
- Main integration SHA: `6f9be336ace0e2e8a48ffa3c7001e3af49e3e490`
- Priority: P1 generated-output health completeness

## Confirmed defect

Both production Curtain Panel writers persist `GeneratedCurtainPanelAreaM2` from the bounded panel plan using exact `double.ToString("R", CultureInfo.InvariantCulture)`. `GeneratedCurtainPanelHealthService.Inspect(...)` validated panel depth, source length and height but did not validate the persisted area snapshot. Missing, malformed, NaN/Infinity, negative, or writer-noncanonical area metadata could therefore remain health-clean.

Zero area is valid because a successful opening-aware panel build can legitimately produce zero remaining panel area/pieces, so this field is finite and non-negative rather than strictly positive.

## Integrated contract

- `GeneratedCurtainPanelAreaM2` must exist, parse invariantly, remain finite, and be `>= 0`.
- Missing/malformed/NaN/Infinity/negative values emit Warning `CURTAIN_PANEL_AREA_INVALID`.
- Otherwise the stored token must equal `value.ToString("R", CultureInfo.InvariantCulture)` ordinally; aliases emit Error `CURTAIN_PANEL_AREA_NON_CANONICAL`.
- Canonical positive and zero-area snapshots remain accepted.
- Existing handles/counts/build-state/fingerprint/mode/stale/native behavior was not changed.
- Health inspection does not rewrite persisted metadata.

## Regression evidence

`tests/QS3D.Core.SmokeTests/GeneratedCurtainPanelAreaHealthSmoke.cs` is auto-registered with a module initializer and covers canonical positive/zero area, missing/malformed/non-finite/negative values, plus explicit-plus/padded/trailing-zero aliases.

PR #857 was reviewed as exactly two changed files and squash-merged with expected head `ecc91e7cabfbd229d3579bea35473c1c5c602dfe` as `6f9be336ace0e2e8a48ffa3c7001e3af49e3e490`.

## Validation boundary

Source and regression were integrated/read back through GitHub. No GitHub Actions/full local .NET build/executable smoke or licensed BricsCAD V25/V26 runtime PASS is claimed without execution.
