# QS3D full-domain BricsCAD V25 runtime checklist

This checklist starts only after the self-hosted `bricscad-v25` integration build succeeds. GitHub-hosted Core CI and source preflight do not count as a BricsCAD runtime pass.

## 1. Build and load
- Windows x64 self-hosted runner with licensed BricsCAD V25.
- `BRICSCAD_V25_DIR` points at the installed V25 directory containing `BrxMgd.dll` and `TD_Mgd.dll`.
- Build `QS3D.BricsCAD.V25` Release/net48/x64 without copying vendor assemblies.
- Package with `scripts/package-full-domain-v25.ps1`.
- Confirm ZIP contains only QS3D assemblies/readme and no BricsCAD DLLs.
- NETLOAD the packaged `QS3D.BricsCAD.V25.dll`.
- Run `QS3D`, `QS3DDOMAIN`, `QS3DRUNTIMEPROBE`.

## 2. UI / lifecycle
- Show/hide the main palettes and Full Domain Hub repeatedly.
- Open/switch/close multiple DWGs and verify project context follows the active document.
- Verify Recognition and Revision modeless windows close cleanly.
- Test Vietnamese Unicode and DPI 100%, 125%, 150%, 200%.
- Compare the left workflow / native central viewport / right manager composition against the approved BLT3D-inspired reference; do not claim pixel parity until screenshots are captured on V25.

## 3. Structural semantic quantities
- `QS3DBEAM`: LINE with Length/Width/Height and concrete/formwork result.
- `QS3DSLAB`: closed polyline Area/Perimeter/Thickness and opening deduction.
- `QS3DCOLUMN`: closed polyline Area+Perimeter+Height fallback and rectangular Width+Depth path.
- `QS3DSTRUCTWALL`: LINE Length/Height/Thickness and linked opening deduction.
- `QS3DFOUNDATION`: closed polyline area/perimeter + thickness/height.
- `QS3DSTAIR`, `QS3DRAILING`, `QS3DEARTHWORK`; verify excavation loose volume with swell factor.
- Model Health must report missing required dimensions/material instead of silently accepting incomplete elements.

## 4. Structural Solid3d
- Capture Beam/Structural Wall semantics from LINE, then run `QS3DSTRUCTSOLID`.
- Capture Slab/Column/Foundation from closed polylines, then run `QS3DSTRUCTSOLID`.
- Verify geometry orientation, elevation/offset, dimensions and layer inheritance.
- Re-run on the same source and verify live `GeneratedStructuralSolidHandle` prevents duplicates.
- Delete a generated solid, rerun, and verify stale handle recovery creates one replacement.
- Undo/redo and transaction-close regression on private sample DWGs.

## 5. Recognition
- Run `QS3DRECOGNIZE` on known Beam/Slab/Column/Wall/Door/Room/Foundation/Earthwork layers and nearby text.
- Verify Vietnamese diacritics normalization.
- Verify low-confidence or narrow-margin rows stay review-required.
- `QS3DRECOGNIZEAUTO` may auto-apply only high-confidence rows; ambiguous rows must remain in the review window.
- Apply reviewed rows and confirm source handle, active Zone/Floor/Family and quantities are persisted.

## 6. Rebar / BBS / BQ
- Existing `QS3DBBS` XLSX workflow must remain functional.
- `QS3DBBSCSV` exports UTF-8 CSV with notation, diameter, count, cut/total length, kg/m, net/waste/total kg.
- Test count notation and spacing notation with distribution length.
- BQ/XLSX must include the `Thép (kg)` column and preserve frozen header + AutoFilter.
- Invalid rebar notation must surface through Model Health and must not crash BQ aggregation.

## 7. Revision / persistence
- Save/reopen `.qsdb` and validate backup recovery/protected no-overwrite state.
- Run `QS3DREVBASE`; verify `.qsrev` persists id, timestamp, category/family/floor/zone, handles, properties and quantities.
- Modify structural/rebar properties/quantities and run `QS3DREVDIFF`.
- Verify Added/Removed/Changed rows, field-level Before/After values and Locate.
- Repeat after reopening BricsCAD to prove the baseline is not memory-only.

## 8. Release qualification
- Run private sample-DWG quantity regression.
- Capture UI screenshots and compare at the DPI matrix.
- Run repeated load/unload/open/close stress tests.
- Test large model performance corpus.
- Only after Gate C/D/E are green may automatic PR CI, signing, installer/auto-update or release-candidate publication be enabled.
