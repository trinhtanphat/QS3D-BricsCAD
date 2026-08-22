# Full-domain integration status — 2026-08-10

## Source integrated on top of latest parallel runtime/structural work

This batch intentionally preserves the newer `main` implementation for runtime probe/selection synchronization, advanced structural categories, rich rebar schedule/BBS XLSX, rich revision delta model and polished workspace. It adds only compatible/unique full-domain pieces instead of replacing the parallel work.

### Core
- rule-based RecognitionEngine: layer + nearby text + entity type, Vietnamese normalization, confidence and top-two margin;
- persistent rich `.qsrev` RevisionSnapshotStore preserving floor/zone/handles/properties/quantities;
- BBS UTF-8 CSV exporter using the current rich RebarScheduleRow contract;
- BQ steel aggregation from ProjectRebarScheduleBuilder and `Thép (kg)` XLSX column;
- Column Area+Perimeter fallback, Earthwork swell/loose-volume quantities;
- Model Health structural/earthwork dimension checks while retaining current material/rebar/recovery/orphan checks;
- full-domain integration smoke suite.

### BricsCAD V25 source
- idempotent semantic capture entry point for recognition;
- safe text/name/tag metadata collection from selected CAD entities;
- Recognition review modeless window;
- persistent Revision baseline/diff modeless window;
- explicit `QS3DSTRUCTSOLID` source path for Beam/Structural Wall LINE and Slab/Column/Foundation closed polylines;
- `QS3DBBSCSV`, `QS3DREVBASE`, `QS3DREVDIFF`, `QS3DRECOGNIZE`, `QS3DRECOGNIZEAUTO` commands;
- `QS3DDOMAIN` modeless Full Domain Hub so the new workflows are accessible without overwriting concurrently maintained Ribbon code;
- guarded full-domain V25 packaging script.

## Verified Core gate

Integration Core run `31343984922` passed preflight, Release build of `QS3D.Core`, and deterministic smoke tests after two real integration issues were fixed:
- compiler mismatch between the older proposed rebar group API and the current `RebarGroup` contract;
- legacy XLSX regression expected 16 columns (`P`) while the integrated BQ now intentionally has 17 columns (`Q`) including steel kg.

The test was updated to require `Thép (kg)` and `A1:Q2`; compiler/nullability strictness was not reduced.

## V25 runtime boundary

The current BricsCAD adapter additions are source-implemented but are **not claimed runtime-verified**. Gate C probe `31341184031` still has no assigned `[self-hosted, windows, x64, bricscad-v25]` runner, so an exact V25 plugin compile/NETLOAD has not run.

`StructuralSolidBuilder` uses V25 `Solid3d` source paths and remains specifically runtime-gated. The final checklist is `docs/FULL-DOMAIN-RUNTIME-CHECKLIST.md`.

## Release policy

- No BLT/BLT3D source/assets/binaries.
- No `BrxMgd.dll`, `TD_Mgd.dll`, or `TD_MgdBrep.dll` in Git or release ZIP.
- No private DWG/DXF/DOCX fixtures in the public repository.
- Main workflows remain manual-only until V25 runtime gates pass.
- Temporary branch-scoped workflow triggers are permitted only for isolated proof gates and are never merged.
