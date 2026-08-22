# CI readiness gate

**Automatic CI on `main` remains intentionally disabled.** Both repository workflows on `main` stay `workflow_dispatch` only. Temporary branch-scoped `push` triggers may be used only to prove a gate when the connector cannot dispatch a manual workflow; those temporary workflow changes are never merged to `main`.

## Gate A — source/static review — PASS

Current preflight covers:
- complete full-domain architecture/persistence/structural/rebar/recognition/revision/UI/test/package files;
- no BricsCAD/BLT proprietary binaries and no private DWG/DXF/DOCX in the public repository;
- XML/XAML parsing, required code-behind files and event handlers;
- C# delimiter sanity;
- net48 adapter guards for incompatible `ToHashSet` use and stale/nonexistent formula APIs;
- packaging guard that rejects BricsCAD vendor assemblies;
- no placeholder UX strings;
- manual-only workflows on the release tree.

## Gate B — hosted Core CI — PASS

Green validation history:
1. `31341101835` — baseline Core;
2. `31341548469` — persistence/export hardening;
3. `31341704360` — hardening snapshot;
4. `31342458832` — structural/rebar/recognition/revision Core after fixing compiler-reported nullable issues;
5. `31342976121` — full-domain snapshot;
6. **`31343166796` — release-tree gate: PASS**.

The release-tree gate passed:
- preflight;
- `QS3D.Core` Release build;
- deterministic smoke tests.

The current suite covers geometry/units/formulas, quantity rules, structural Beam/Slab/Column/Structural Wall/Foundation/Earthwork calculations, generic takeoff, rebar notation/BBS/weight, recognition confidence/review behavior, project BQ steel aggregation, revision quantity diff + persistent revision store, QSDB migration/backup/recovery/locking, XLSX/CSV packaging and structural/earthwork/rebar Model Health validation.

## Gate C — BricsCAD V25 integration build — BLOCKED BY RUNNER

Probe run `31341184031` is queued for:

`[self-hosted, windows, x64, bricscad-v25]`

with no assigned runner. This does **not** represent a failed plugin build; the V25 job has not started.

Gate C requires:
- Windows x64 self-hosted GitHub Actions runner;
- labels `self-hosted`, `windows`, `x64`, `bricscad-v25`;
- licensed BricsCAD V25 installation;
- repository variable `BRICSCAD_V25_DIR` pointing at the local V25 installation directory;
- local `BrxMgd.dll` and `TD_Mgd.dll` under that path;
- no vendor DLL committed or uploaded as a project artifact.

See `docs/V25-RUNNER.md`.

## Gate D — interactive runtime — PENDING GATE C

After a Gate C artifact exists, execute `docs/RUNTIME-TEST-CHECKLIST.md`, including:
- NETLOAD, Ribbon, left/right palettes and multi-DWG lifecycle;
- LINE → architectural wall / Beam / Structural Wall;
- closed polyline → Room / Slab / Column / Foundation;
- Room → finishes;
- Opening/Door → Host Link → quantity deduction;
- Earthwork quantity;
- Rebar/BBS/CSV and BQ steel kg;
- recognition review/auto-confidence behavior;
- `.qsdb` save/reopen/backup recovery and `.qsrev` revision baseline/diff;
- Layer/Xref manager and Locate;
- Unicode, DPI 100/125/150/200%, repeated load/unload and clean BricsCAD shutdown;
- package script + NETLOAD from packaged folder.

## Gate E/F — pending runtime

Private sample-DWG regression, generated-solid lifecycle/undo tests, UI screenshot comparison, performance corpus, installer/code signing/update tests and release-candidate qualification. Automatic PR CI or production release should stay disabled until the relevant runtime gates are green.
