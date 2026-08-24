# Local SaveAs lifecycle qualification

Status: `PENDING_LOCAL` / `DO_NOT_RETRY_REMOTE`

Parent: #72 / #3719  
Source-preparation carrier: #3751

This bounded qualification proves the native BricsCAD `SAVEAS` transition for a QS3D project using only the repository-generated `samples/generated/QS3D-Sample.dwg`. It does not use or authorize customer/private drawings.

## What the runner proves

On one clean exact pushed SHA, the runner:

1. copies the generated sample into a new isolated `artifacts/**` directory;
2. validates the BricsCAD host major and matching QS3D adapter assembly major;
3. validates plugin SourceLink identity against exact `git rev-parse HEAD`;
4. creates and persists a baseline QS3D project beside the source copy;
5. stages a second semantic metadata mutation so the project is genuinely pending;
6. executes native `SAVEAS` to a new drawing path;
7. requires SaveComplete persistence to create the target `.qsdb` while preserving the original `.qsdb` bytes;
8. verifies the same canonical ProjectId through a salted digest without writing the raw ProjectId to evidence;
9. requires pending state to be cleared;
10. forgets the in-memory project cache and reloads the target sidecar, requiring the same identity and persisted mutation;
11. emits only sanitized boolean/hash/version evidence and removes the launched BricsCAD process.

A source/static/CI pass is not licensed runtime evidence. Do not mark this row `LOCAL_PASS` until the runner actually succeeds in the required host.

## V25 command

From a clean checkout at the exact intended SHA, with no matching BricsCAD process running:

```powershell
.\scripts\test-bricscad-saveas-lifecycle.ps1 `
  -BricsCadDir 'C:\Program Files\Bricsys\BricsCAD V25 en_US' `
  -PluginDll '.\src\QS3D.BricsCAD.V25\bin\x64\Release\net48\QS3D.BricsCAD.V25.dll' `
  -FixtureDwg '.\samples\generated\QS3D-Sample.dwg' `
  -Profile 'QS3D-V25-TEST' `
  -ArtifactDir '.\artifacts\local-saveas-v25\run' `
  -ExpectedHostMajor 25 `
  -ConfirmSyntheticFixture
```

## V26 command

Build the V26 adapter first against the licensed V26 install, then run:

```powershell
.\scripts\test-bricscad-saveas-lifecycle.ps1 `
  -BricsCadDir $env:BRICSCAD_V26_DIR `
  -PluginDll '.\src\QS3D.BricsCAD.V26\bin\x64\Release\net8.0-windows\QS3D.BricsCAD.V26.dll' `
  -FixtureDwg '.\samples\generated\QS3D-Sample.dwg' `
  -Profile 'QS3D-V26-TEST' `
  -ArtifactDir '.\artifacts\local-saveas-v26\run' `
  -ExpectedHostMajor 26 `
  -ConfirmSyntheticFixture
```

## Required evidence

Keep `saveas-lifecycle-metadata.json` local/untracked and report only sanitized values:

- exact Git SHA;
- expected host major and BricsCAD file version;
- plugin SHA-256;
- unchanged repository fixture SHA-256;
- `nativeSaveAsPathTransition=true`;
- `canonicalProjectIdentityPreserved=true`;
- `targetSidecarPersisted=true`;
- `originalSidecarUnchanged=true`;
- `pendingStateCleared=true`;
- `coldCacheReloadMatched=true`;
- zero matching BricsCAD process residue.

Do not publish raw drawing paths, ProjectIds, Handles, private fixture names, proprietary DLLs, screenshots containing private data, or unsanitized logs.

PASS closes only this bounded SaveAs lifecycle cell. FAIL must be reported as sanitized exact-SHA source/runtime evidence and returned to a separate source-fix lane when the failure is a production defect. NO_RESULT remains pending and must never be interpreted as PASS.
