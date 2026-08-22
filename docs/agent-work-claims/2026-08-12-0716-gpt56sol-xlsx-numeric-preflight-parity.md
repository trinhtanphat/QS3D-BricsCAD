# Work claim — XLSX numeric preflight parity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-xlsx-numeric-preflight-parity-20260812-0716`
- Registered: `2026-08-12T07:16:00+07:00`
- Completed: `2026-08-12T07:28:00+07:00`
- Baseline main SHA: `88574b56ad2bc6b07c383545afad9a88f46be9fd`
- Integrated main SHA: `55692f337fc9278852880ca3ebd473643e9c8016`
- PR: `#616`
- Priority: evidence-driven remote-safe export atomicity hardening during owner-requested `continue all`

## Completed scope

Door/Opening, Material, Curtain, Room Finish and BBS/Rebar XLSX exporters now reject non-finite numeric cells during row preflight before path resolution, directory creation, temp-package creation or worksheet serialization.

## Changes

- Door/Opening: preflight `WidthM`, `HeightM`, `SillHeightM`, `ThicknessM` and `OpeningAreaM2`.
- Material: preflight `PrimaryQuantity`, `LengthM`, `AreaM2`, `VolumeM3` and `MassKg`.
- Curtain: preflight all emitted floating-point wall/glass/frame/panel-clear metrics.
- Room Finish: preflight `PrimaryQuantity`, `LengthM` and `AreaM2`.
- BBS/Rebar: preflight `DiameterMm`, `CuttingLengthM`, `TotalLengthM`, `UnitWeightKgM`, `NetWeightKg`, `WastePercent` and `TotalWeightKg`.
- Each failure identifies `rows`, worksheet row and field.
- Existing serializer-level finite checks remain as defense in depth.
- Added `XlsxScheduleNumericPreflightSmoke` covering one non-finite rejection per exporter before invalid destination-directory creation plus ordinary finite export paths for all five.

## Excluded scope

- `XlsxQuantityExporter.cs` remained outside this lane. Its pre-existing structural-limits claim resumed concurrently, and a separate Quantity XLSX XML-sanitization claim also appeared while this lane was active.
- XML text sanitization, text-cell limits, worksheet row limits, null-row handling, reporting/grouping/business rules, sign/domain validation beyond the existing finite-number contract, and shared XLSX package validation were unchanged.
- Native BricsCAD/UI/runtime work, GitHub Actions, release packaging and LOCAL_ONLY qualification were not performed.

## Validation actually performed

- Published and corrected the claim before any product-source changes.
- Reviewed the exact feature-branch diff: exactly five exporter files plus one focused smoke file.
- Re-read all five owned exporter blobs on moving `main`; none changed between the branch base and pre-integration `main`, despite 74 concurrent commits in other lanes.
- Reviewed PR #616 patch after publication.
- Confirmed exact PR head `5e1f589771f2c069a52110e70a987e25be0aa4d1` had no pull-request workflow runs; no Actions were dispatched.
- First exact-head merge attempt was rejected because `main` moved. Refreshed the base, confirmed the four new commits touched only Quantity claims/tests and semantic-sheet coordination, then retried the same exact head successfully.
- Squash-merged PR #616 into `main` as `55692f337fc9278852880ca3ebd473643e9c8016`.
- Re-read merged Door/Opening and BBS/Rebar source plus the dedicated smoke from remote `main` after integration; the merged finite preflight and regression source are present.
- No local `.NET` build/test, licensed BricsCAD V25/Windows/native runtime execution or `LOCAL_PASS` is claimed from this connector-only environment.

## Integration

PR #616 was squash-merged into `main` as `55692f337fc9278852880ca3ebd473643e9c8016` without force-push.
