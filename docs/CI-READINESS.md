# CI readiness gate

**Automatic CI on `main` remains intentionally disabled.** Both repository workflows on `main` stay `workflow_dispatch` only. Temporary branch-scoped `push` triggers may be used only to prove a gate when the connector cannot dispatch a manual workflow; those temporary workflow changes are never merged to `main`.

## Gate A — source/static review — PASS

Current guards cover:
- required architecture/persistence/UI/test files;
- no BricsCAD/BLT proprietary binaries and no private DWG/DOCX in the public repository;
- XML/XAML parsing and code-behind event handlers;
- C# delimiter sanity;
- net48 adapter guards for incompatible `ToHashSet` use and stale/nonexistent formula APIs;
- no placeholder UX strings;
- manual-only workflows on the release tree.

## Gate B — Core CI — PASS

GitHub-hosted Windows validation has passed on the baseline and both hardening snapshots:

1. baseline Core gate — Actions run `31341101835`;
2. persistence/export hardening gate — Actions run `31341548469`;
3. final hardening snapshot gate — Actions run `31341704360`.

The final gate passed:
- preflight;
- `QS3D.Core` Release build;
- deterministic smoke tests.

The hardening suite covers:
- QSDB schema v1 → v2 migration;
- validated temp save + backup recovery;
- duplicate-ID rejection;
- project health recovery states;
- corrected Excel quantity headers, frozen header row and AutoFilter.

## Gate C — BricsCAD V25 integration build — BLOCKED BY RUNNER

A real probe was created as Actions run `31341184031`. The plugin job remained queued with labels:

`[self-hosted, windows, x64, bricscad-v25]`

and no assigned `runner_id` / `runner_name`. This means Gate C has **not failed the plugin build**; it has not started because no matching self-hosted runner is currently available to the repository.

Gate C requires:
- Windows x64 self-hosted GitHub Actions runner;
- labels `self-hosted`, `windows`, `x64`, `bricscad-v25`;
- licensed BricsCAD V25 installation;
- repository variable `BRICSCAD_V25_DIR` pointing at that installation directory;
- installed `BrxMgd.dll` and `TD_Mgd.dll` under that path;
- no vendor DLL uploaded to GitHub.

See `docs/V25-RUNNER.md`.

## Gate D — interactive runtime test — PENDING GATE C

After a Gate C build artifact exists:
- NETLOAD in BricsCAD V25;
- Ribbon and left/right palettes;
- multi-DWG create/activate/close;
- LINE → Tường KT semantic + Solid3d;
- closed polyline → Room → HT_Phòng;
- Opening/Door capture + Host Link + quantity deduction;
- Layer/Xref manager;
- BQ Locate + XLSX;
- `.qsdb` save/reload, backup recovery and Model Health;
- repeated open/close, Unicode and DPI 100/125/150/200%;
- close BricsCAD without dispose exceptions.

## Gate E/F — pending runtime

Private sample-DWG quantity regression, persistence/reopen regression, UI screenshot comparison, performance corpus, packaging and installer tests. Only after these are green should automatic PR CI or a release candidate be enabled.
