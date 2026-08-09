# CI readiness gate

**Automatic CI is intentionally disabled.** Both workflows are `workflow_dispatch` only.

## Gate A — repository/static review (required before any Actions run)
- Preflight passes locally.
- No BricsCAD/BLT proprietary binaries or user DWGs/DOCX are committed.
- XAML parses as XML.
- Event handlers declared in XAML exist in code-behind.
- Core source delimiter/static checks pass.
- Workflows contain no `push`/`pull_request` trigger.

## Gate B — manual Core CI
Run `QS3D Core CI` manually on GitHub hosted `windows-latest`.
Expected:
- `QS3D.Core` compiles.
- deterministic smoke tests pass.
- XLSX exporter package structure test passes.

## Gate C — manual BricsCAD V25 integration build
Requires a licensed Windows self-hosted runner labelled `bricscad-v25` and repository variable `BRICSCAD_V25_DIR`.
Expected:
- BrxMgd.dll and TD_Mgd.dll are referenced from the V25 installation and never uploaded.
- plugin net48/x64 build succeeds.
- artifact contains only QS3D assemblies.

## Gate D — interactive runtime smoke test
Only after Gate C:
- NETLOAD in BricsCAD V25.
- open/close palettes repeatedly.
- inspect selection on LINE/POLYLINE/BLOCK/HATCH/DIM/TEXT where applicable.
- multi-DWG switching.
- UI scaling 100/125/150%.
- Vietnamese Unicode.
- BQ window and XLSX export.
- close BricsCAD with no dispose exception.

## Automatic CI policy
Do not add `push` or `pull_request` triggers until Gates A-D pass on the target V25 environment.
