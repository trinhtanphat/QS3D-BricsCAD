# CI readiness gate

**Automatic CI is intentionally disabled.** Both workflows remain `workflow_dispatch` only. Do not add `push` or `pull_request` triggers without explicit approval after V25 runtime gates pass.

## Gate A — source/static review before Actions

Required:
- `scripts/preflight.py` source guard is reviewed for the current tree;
- no BricsCAD/BLT proprietary binaries and no private DWG/DOCX are committed;
- XAML/XML and code-behind handler guards cover all current windows/palettes;
- net48 adapter guard rejects `Enumerable.ToHashSet` usage and nonexistent formula API names;
- workflows contain only manual dispatch.

## Gate B — manual Core CI

Only when explicitly approved, run `QS3D Core CI` on GitHub-hosted `windows-latest`:
- preflight;
- build `QS3D.Core`;
- deterministic smoke tests including geometry, units, formulas, rebar notation, reports/XLSX, dependency/regeneration, `.qsdb`, health, revision and lock behavior.

## Gate C — manual BricsCAD V25 integration build

Requires a licensed Windows self-hosted runner labelled `bricscad-v25` and repository variable `BRICSCAD_V25_DIR`:
- validate installed `BrxMgd.dll` / `TD_Mgd.dll`;
- build `net48/x64` plugin;
- artifact contains QS3D assemblies only.

## Gate D — interactive runtime test

- NETLOAD in BricsCAD V25;
- Ribbon and left/right palettes;
- multi-DWG create/activate/close;
- LINE → Tường KT semantic + Solid3d;
- closed polyline → Room → HT_Phòng;
- Opening/Door capture + Host Link + quantity deduction;
- Layer/Xref manager;
- BQ Locate + XLSX;
- `.qsdb` save/reload and Model Health;
- repeated open/close, Unicode and DPI 100/125/150/200%;
- close BricsCAD without dispose exceptions.

## Gate E/F

Private sample-DWG quantity regression, persistence/reopen regression, UI screenshot comparison and performance corpus. Only after these are green should automatic PR CI or a release candidate be enabled.
