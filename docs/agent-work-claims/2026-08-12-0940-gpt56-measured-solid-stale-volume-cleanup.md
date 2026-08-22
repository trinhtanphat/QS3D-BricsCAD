# Work claim — Measured solid stale volume cleanup

- Status: `ACTIVE`
- Agent: `gpt-5.6-sol-chatgpt`
- Registered: `2026-08-12T09:40:30+07:00`
- Baseline main SHA: `9837809e6c06b86b0c89d10f630441954dbd7bec`
- Priority: owner-requested continue-all source-safe bug fixing

## Reserved scope

Fix stale derived measured-volume quantities in `MeasuredSolidQuantityPolicy.Apply()`: when measured solid volume is absent or the element category no longer supports material volume, quantities previously derived by this policy must not survive as stale `MeasuredSolidVolumeM3` / `GrossVolumeM3` / `NetVolumeM3` values.

## Expected surfaces

- `src/QS3D.Core/Services/MeasuredSolidQuantityPolicy.cs`
- focused smoke coverage under `tests/QS3D.Core.SmokeTests/`
- `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs` only if a new smoke class must be registered

## Excluded scope

- quantity XLSX preflight/export lanes
- Quantity Summary Follow3D/UI work
- quantity-rule evaluation semantics
- persistence/backup/sidecar work
- BricsCAD V25/V26 runtime, packaging, signing, private DWG, GitHub Actions

## Evidence

Current `MeasuredSolidQuantityPolicy.Apply()` writes `MeasuredSolidVolumeM3`, `GrossVolumeM3`, and `NetVolumeM3` together when `MeasuredSolidVolumeM3` source data is present. On a later apply with no applicable measured volume, it removes only `MeasuredSolidVolumeM3`, leaving the two derived gross/net values stale even though their measured source disappeared.

## Intended validation

Add focused Core smoke coverage for removal of all policy-owned derived volume outputs when the measured source is removed or becomes inapplicable, while preserving surface-area behavior and ordinary successful measured-volume application.
