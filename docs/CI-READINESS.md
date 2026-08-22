# CI readiness gate

## Current trigger policy

GitHub Actions are **manual-only by default**, with exactly one owner-approved automatic exception: `.github/workflows/dispatch-v25-cloud-after-main-integration.yml` may react to an integration-relevant `main` landing and dispatch only `release-v25-cloud.yml`.

This supersedes the historical statement that automatic CI on `main` is completely disabled. All other workflows remain `workflow_dispatch`-only unless the repository owner explicitly changes policy again.

The dispatcher is path-filtered. Ordinary docs/Markdown-only landings outside its watched source/test/script/build/workflow paths do not start the V25 cloud release lane. Changed paths, not commit-message prefixes such as `docs:` or `chore:`, are authoritative.

Normal agents still treat `main` as read-only unless the owner explicitly authorizes that session to merge/integrate the named PR/batch. `continue all`, `fix bug`, `update code`, `commit push git`, docs/chore work, CI work or review does not imply `main` merge permission.

## Gate A — source/static review — PASS

Current guards cover:
- required architecture/persistence/UI/test files;
- no BricsCAD/BLT proprietary binaries and no private DWG/DOCX in the public repository;
- XML/XAML parsing and code-behind event handlers;
- C# delimiter sanity;
- net48 adapter guards for incompatible `ToHashSet` use and stale/nonexistent formula APIs;
- no placeholder UX strings;
- CI trigger policy and release safety guards.

## Historical Gate B — Core CI — PASS for recorded snapshots

GitHub-hosted Windows validation passed on these historical snapshots:

1. baseline Core gate — Actions run `31341101835`;
2. persistence/export hardening gate — Actions run `31341548469`;
3. final hardening snapshot gate — Actions run `31341704360`.

Those runs remain evidence only for their own source trees. They do not qualify a newer current `main` tree.

The recorded final gate passed:
- preflight;
- `QS3D.Core` Release build;
- deterministic smoke tests.

The hardening suite covered:
- QSDB schema v1 → v2 migration;
- validated temp save + backup recovery;
- duplicate-ID rejection;
- project health recovery states;
- corrected Excel quantity headers, frozen header row and AutoFilter.

## Historical Gate C — BricsCAD V25 integration build — BLOCKED BY RUNNER at recorded probe

A recorded probe was created as Actions run `31341184031`. The plugin job remained queued with labels:

`[self-hosted, windows, x64, bricscad-v25]`

and no assigned `runner_id` / `runner_name`. That historical probe did **not** prove a plugin build failure; it did not start because no matching self-hosted runner was available at that time.

Gate C requires:
- Windows x64 self-hosted GitHub Actions runner;
- labels `self-hosted`, `windows`, `x64`, `bricscad-v25`;
- licensed BricsCAD V25 installation;
- repository variable `BRICSCAD_V25_DIR` pointing at that installation directory;
- installed `BrxMgd.dll` and `TD_Mgd.dll` under that path;
- no vendor DLL uploaded to GitHub.

See `docs/V25-RUNNER.md`.

## Gate D — interactive runtime test

For an exact candidate with a valid V25 build artifact, licensed runtime qualification includes:
- NETLOAD / DemandLoad in BricsCAD V25;
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

Use the current local qualification runbook and exact-SHA evidence. A historical runner state must not be treated as the current runner state without a fresh check.

## Gate E/F — runtime/release evidence

Private sample-DWG quantity regression, persistence/reopen regression, UI screenshot comparison, performance corpus, packaging, installer and signing tests remain environment-specific evidence classes.

The automatic V25 cloud dispatcher does not replace these licensed/private/local gates. Only report them as PASS after the corresponding exact candidate payload was actually exercised.
