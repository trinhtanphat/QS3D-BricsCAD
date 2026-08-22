# QS3D LOCAL-003 — exact-SHA Level Z-chain qualification

Updated: 2026-08-14 (UTC+7)

This is the focused local runbook for the shared native Level vertical-placement chain. It supplements `docs/LOCAL-V25-QUALIFICATION.md`; it does not create a second live queue. Current priority and status remain in `docs/LOCAL-AGENT-INBOX.md` under `LOCAL-003`.

## Truth boundary

Keep these three states separate:

1. `SOURCE_INTEGRATED_CANDIDATE`: deterministic Core/static contracts and all applicable native/dependent consumers are wired to the shared placement service.
2. `AUTOMATED_RUNTIME_PROBE_PASS`: the guarded representative probe passed inside licensed BricsCAD V25 on a clean exact SHA and matching DLL.
3. `FULL_LOCAL_MATRIX_PASS`: mm/m, complete family/dependent coverage, Undo, save/reopen, multi-DWG and representative private-DWG scenarios passed on that same SHA/DLL.

Only state 3 can close `LOCAL-003`. Neither source review nor the focused probe alone is customer-release qualification.

## Prerequisites

- interactive Windows session with licensed BricsCAD V25 x64;
- no running BricsCAD process;
- current `main` fetched and integrated, clean worktree, exact 40-character `HEAD` SHA;
- Core and `QS3D.BricsCAD.V25` Release assemblies built from that exact SHA against the installed V25 managed assemblies;
- a disposable repository-generated drawing copy named `*.level-z-probe-copy.dwg` with no `.qsdb` or `.qsdb.bak` sidecar;
- all output under gitignored `artifacts/`.

Do not use an original or private production drawing for the automated probe. Do not copy BricsCAD DLLs into the repository.

## Prepare and run the focused probe

From an interactive PowerShell session at the repository root:

```powershell
git status --short
$sourceSha = git rev-parse HEAD
$levelRoot = Join-Path $PWD "artifacts\local-v25-level-z"
New-Item -ItemType Directory -Force -Path $levelRoot | Out-Null
Copy-Item `
  -LiteralPath ".\samples\generated\QS3D-Sample.dwg" `
  -Destination (Join-Path $levelRoot "QS3D-Sample.level-z-probe-copy.dwg")

$env:BRICSCAD_V25_DIR = "C:\Program Files\Bricsys\BricsCAD V25 en_US"
dotnet build ".\src\QS3D.BricsCAD.V25\QS3D.BricsCAD.V25.csproj" -c Release -p:Platform=x64
Remove-Item Env:BRICSCAD_V25_DIR

.\scripts\test-bricscad-v25-level-z.ps1 `
  -BricsCadDir "C:\Program Files\Bricsys\BricsCAD V25 en_US" `
  -PluginDll ".\src\QS3D.BricsCAD.V25\bin\x64\Release\net48\QS3D.BricsCAD.V25.dll" `
  -DrawingCopy (Join-Path $levelRoot "QS3D-Sample.level-z-probe-copy.dwg") `
  -Profile "QS3D-V25-TEST" `
  -ArtifactDir (Join-Path $levelRoot "run") `
  -ExpectedSourceSha $sourceSha `
  -ConfirmDisposableCopy
```

The runner fails closed when the worktree is dirty, `HEAD` differs from `-ExpectedSourceSha`, plugin/Core `ProductVersion` does not end in that SHA, the drawing suffix is wrong, a sidecar already exists, another BricsCAD process is open, output already exists, or the drawing hash/sidecar state changes.

## Automated probe acceptance

The nonce-bound `QS3DLEVELZPROBE` must report:

- legacy wall bottom/top: `1.2 / 3.7 m`;
- Bottom+Top wall bottom/top: `3.1 / 6.8 m`, despite intentionally invalid ignored legacy inputs;
- Bottom-only Beam bottom/top: `3.25 / 3.85 m`;
- Top-only placement refusal before native ownership mutation;
- bounded Door physical cut reduces positive host volume;
- positive Curtain frame/panel counts contained by the bounded GlassWall range;
- exactly four Beam longitudinal bars and positive stirrup count, both contained by the Beam range;
- effective Beam quantity height `0.6 m` and matching generated vertical snapshots;
- zero Level health issues before Level edits, then stale snapshot/dependent invalidation after Top and Bottom Level changes;
- exact source SHA/64-bit BricsCAD marker, unchanged disposable-DWG SHA-256 and no sidecar after the process exits.

The runner writes sanitized marker/metadata only under the chosen artifact directory. Keep those files local unless a bounded sanitized summary is useful in Git.

## Exact representative probe result

`AUTOMATED_RUNTIME_PROBE_PASS` was recorded on clean exact SHA `2a1967d66f005cbdef20bb024a7b92a7f44077cc` with matching plugin/Core ProductVersion, BricsCAD V25.2.10 x64 and plugin SHA-256 `86D3938C89BB42BBE4F4854F7C8C027736426B5B20648363798ED634D811516F`. Full Core smoke, all focused Level gates and the installed-reference V25 `Release|x64` build passed before launch.

The sanitized marker verified legacy wall `1.2..3.7 m`, Bottom+Top wall `3.1..6.8 m`, Bottom-only Beam `3.25..3.85 m`, physical-opening reduction, 16 Curtain frames, 14 Curtain panels, four longitudinal bars, six stirrups, zero pre-edit Level health issues, seven stale outputs after the Level edit, Level-edit invalidation and Top-only fail-closed behavior. The host exited gracefully; process, generated script and private drawing state were absent; the disposable drawing was restored exactly to SHA-256 `CEC1350FB2207542AEECD96A790A198A6C9CC9E99A9F875871F367554B3D967E`.

This is state 2 only. The follow-on matrix below remains `PENDING_LOCAL`; LOCAL-003 stays `IN_PROGRESS` and customer-release qualification remains false.

## Required follow-on interactive matrix

Use the exact same SHA and built DLL. Cover at minimum:

- drawings whose native units are millimetres and metres;
- no-Level legacy geometry and fingerprints for every touched applicable family;
- Bottom-only and Bottom+Top placement for ArchitecturalWall, GlassWall, WallPier, StructuralWall, Beam, Column, Slab, Foundation, Stair and Railing;
- Door and WallOpening host containment, straight and curved cutting, Auto Host, host Level edit and opening Level edit;
- Curtain LINE and path frames/panels/live fingerprints, opening clipping, stale/rebuild and owner safety;
- Beam/Column rebar, ties/stirrups, Slab/Foundation/StructuralWall mesh and Shape rebar alignment;
- Top-only, missing/deleted/ambiguous Level, non-finite offsets and `top <= bottom` refusal before native or semantic partial mutation;
- source reconcile, Health/Release result, Undo/Redo, save/reopen and cold-cache rebind;
- two simultaneously open DWGs with distinct projects and Levels; modeless actions must never mutate the other drawing;
- representative authorized private drawings without committing paths, handles, names, source geometry or screenshots containing private content.

## Minimum close-out evidence

Record a sanitized summary tied to the exact tested SHA and DLL hash/version:

- BricsCAD V25 file version and x64 process result;
- focused runner PASS plus aggregate counts/booleans and before/after disposable hash equality;
- each interactive matrix row with PASS/FAIL and measured Z/range relationship, not raw drawing identity;
- Undo, save/reopen, Health/Release and multi-DWG results;
- any failure, its source fix/regression guard and the new exact SHA used for the rerun.

If source changes after any failure, discard the earlier runtime verdict for release purposes, rebuild and rerun the affected focused and interactive scenarios on the new exact SHA.
