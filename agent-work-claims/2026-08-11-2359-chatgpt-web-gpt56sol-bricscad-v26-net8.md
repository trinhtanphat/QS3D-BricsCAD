# Work claim — BricsCAD V26 .NET 8 compatibility

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-11T23:59:00+07:00`
- Completed: `2026-08-12T00:52:00+07:00`
- Baseline main SHA: `9f4f28d5ed79d3b898c70078eeaeeb345b4fd9ea`
- Priority: Owner explicitly requested support for the latest BricsCAD V26 while preserving the existing V25 lane.

## Reserved scope

Add a real BricsCAD V26 Windows managed-plugin compatibility lane for QS3D, accounting for BricsCAD V26's .NET 8 host architecture. Preserve V25 as a supported backward-compatible build/runtime lane instead of relabeling the existing net48 assembly. Cover source-safe project/build selection, V26 host reference probing, installer/runtime host targeting, deterministic static regression guards, and the minimum canonical documentation/local-qualification updates required by this compatibility change.

## Completed implementation

- Added `src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj` as an isolated `net8.0-windows` x64 assembly named `QS3D.BricsCAD.V26`.
- Reused the established V25 CAD/WPF source tree by linked compile/page items while keeping V25 itself on `net48`; V26 resolves only `BRICSCAD_V26_DIR\BrxMgd.dll` and `TD_Mgd.dll` with `Private=false`.
- Added a V26-specific `PluginEntry` that keeps the established source namespace/XAML contracts but deliberately does not start the V25 updater.
- Added a V26 `QS3DUPDATE` safety stub that refuses to reuse the V25 signed update channel until a V26-specific package/manifest lane is qualified.
- Added `scripts/test-bricscad-v26-runtime.ps1`, which fails closed unless the configured host is BricsCAD major 26, the exact `QS3D.BricsCAD.V26.dll` is loaded, x64 runtime probe succeeds, and Ribbon/palette readiness markers are present.
- Added `scripts/preflight-bricscad-v26.py` to guard V25 preservation, V26 .NET 8/reference isolation, manual CI, runtime identity checks and updater-channel isolation.
- Added `.github/workflows/bricscad-v26.yml` as a `workflow_dispatch`-only self-hosted Windows/x64/`bricscad-v26` build/runtime lane.
- Added `docs/LOCAL-V26-QUALIFICATION.md` with the exact `LOCAL_ONLY` / `DO_NOT_RETRY_REMOTE` licensed V26 qualification matrix.
- Closed superseded PR #578 without merge after its sync history picked up unrelated concurrent commits; the actual V26 files were committed directly to `main` through optimistic-lock GitHub Contents writes so no concurrent work was overwritten.

## Published main commits

- `2d9aab07e953282cc592b5eeada7a6114b3795be` — V26 .NET 8 project.
- `169ef38ecd7d5c0c562ce983acddcce19a1cb653` — V26 update-channel fail-safe.
- `cc91b7e2f803e64f52e969127d1c8569e2d01996` — V26 compatibility preflight.
- `7971342755449491a335dc73ee631bb5e2e864bc` — local V26 qualification runbook.
- `2a6794ed29c98aaa539767f4eafb77a216222283` — manual V26 build/runtime workflow.
- `98b19bb03cae5b58a86a5089ace821fab4d1e6f7` — V26 runtime gate.
- `563b70fec93c0169ad9c25d17ef4eadc185a2373` — V26 plugin entry.

## Validation actually performed

- Re-read the V26 project, plugin entry, update stub, workflow and runtime gate directly from current `main` after publication.
- Re-read the existing `QS3DRUNTIMEPROBE` implementation and confirmed the V26 gate checks marker keys that are actually emitted (`status`, `command`, `process`, `is_64bit`, `assembly`, `ribbon_ready`, `palette_visible`).
- Cross-checked the current Bricsys V26 .NET guidance: V26 uses .NET 8, requires `net8.0-windows`, `BrxMgd.dll` + `TD_Mgd.dll` references, and .NET 8 Desktop Runtime x64 for end users.
- Confirmed the V26 workflow remains manual-only and the V26 project does not resolve references through `BRICSCAD_V25_DIR` or compile the V25 updater implementation.

## Validation not claimed

- No GitHub Actions workflow was dispatched in this lane.
- No licensed BricsCAD V26 `NETLOAD`/DemandLoad, WPF/UI, command, save/reopen or clean-machine installer runtime PASS is claimed remotely.
- No V26 production release/package is claimed. Installer/signing/update-manifest work remains a separate follow-up lane and must respect any active installer/release claims before implementation.

## Coordination outcome

The claim was registered before writing V26 code. While implementation was in progress, new V25 installer/updater claims appeared; this batch therefore narrowed itself away from those owned V25 files and implemented the V26 build/runtime boundary without weakening or overwriting concurrent installer/update work.

## Completion condition

Satisfied for the source/build/runtime-qualification compatibility lane: a coherent V26 .NET 8 project, manual CI lane, runtime identity gate, deterministic static guard and local qualification contract are on `main`, while V25 remains explicitly preserved. Licensed runtime and V26 package/update qualification remain separate LOCAL_ONLY/follow-up work rather than fabricated PASS claims.
