# QS3D BricsCAD V25 runtime gate

This checklist starts only after the self-hosted `bricscad-v25` integration build succeeds. Source-only or GitHub-hosted Core CI does not count as a V25 runtime pass.

## Load / lifecycle
- Build `net48/x64` against the exact installed V25 `BrxMgd.dll` and `TD_Mgd.dll`.
- NETLOAD `QS3D.BricsCAD.V25.dll`.
- Run `QS3D`, `QS3DRIBBON`, `QS3DRESETUI`, `QS3DSAFEMODE`.
- Open, activate, switch and close multiple DWGs; close BricsCAD without dispose/unhandled exceptions.

## UI comparison
- Capture screenshots at DPI 100%, 125%, 150% and 200%.
- Verify dark palette, left model tree/family/property layout, native central CAD viewport, right Drawing/Layer manager and ribbon tabs against the approved BLT3D-inspired reference.
- Verify Vietnamese Unicode, narrow/wide dock sizes and keyboard focus.

## Semantic / geometry
- LINE → `QS3DWALL` and verify semantic quantity + generated wall Solid3d.
- LINE → `QS3DBEAM`; LINE → `QS3DSTRUCTWALL`.
- Closed polyline → `QS3DSLAB`, `QS3DCOLUMN`, `QS3DFOUNDATION`; verify `CreateExtrudedSolid` geometry, elevation and quantity.
- Closed polyline → Room → `QS3DFINISH`.
- Door/Opening → `QS3DLINKHOST` → host deduction.
- Earthwork quantity with depth and swell factor.
- Rebar notation / BBS / CSV and BQ steel kg.

## Recognition / revision / persistence
- Run `QS3DRECOGNIZE` on known layers/text and review ambiguous suggestions.
- Run `QS3DRECOGNIZEAUTO` only on high-confidence samples; verify it never silently applies review-required rows.
- Save/reopen `.qsdb`; corrupt primary and verify `.bak` recovery without overwrite of damaged source.
- `QS3DREVBASE`, modify quantities, `QS3DREVDIFF`, locate changed elements.

## Delivery
- Run `scripts/package-v25.ps1` after a successful plugin build.
- Confirm package contains only QS3D assemblies/readme and no BricsCAD vendor DLLs.
- Smoke NETLOAD from the packaged folder on a clean V25 Windows profile.
