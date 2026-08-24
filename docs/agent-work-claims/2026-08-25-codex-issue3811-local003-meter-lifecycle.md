# Work claim — LOCAL-003 native Meter Level lifecycle

- Status: `LOCAL_PASS / BOUNDED`
- Lane-Key: `issue-3811`
- Issue: #3811
- Parent: #72 / LOCAL-003
- Branch: `agent/codex/issue3811-local003-meter-lifecycle`
- Registration baseline: `origin/main@5c7eb75ffdc1f8e79c019a5046bf6489fbe1a2d8`
- Host gate: licensed BricsCAD V25.2.10 Windows x64

## Qualified bounded row

Qualify the existing representative GlassWall/Curtain Level Undo/save-reopen
lifecycle in a native Meter drawing. Existing accepted lifecycle evidence is
Millimeter-only; the fresh Meter Level evidence currently stops at the focused
no-save representative probe.

The exact pushed candidate must run the same production probe/commands with
`INSUNITS=6` and a 5 m source, then prove Level-resolved `3.1..6.8 m`
host/frame/panel output, coherent native/semantic Undo and Redo, explicit
project/DWG save, graceful first-process close, fresh-process cold reopen,
ownership-scoped rebuild, old-output retirement, disjoint replacement ownership,
stable counts and zero Level/P11 Health issues.

## Exact evidence

The clean pushed runtime candidate
`2b30b81f3dfbaa389e50d1bc2058bcd530572ed4`, based on unchanged
`origin/main@5c7eb75ffdc1f8e79c019a5046bf6489fbe1a2d8`, passed the official
exact-candidate baseline. All `1024/1024` aggregate feature preflights, Core
Release, deterministic Core smoke `ALL PASS`, V25 `Release|x64` with zero
warnings/errors, offline WPF and licensed V25 NETLOAD/Ribbon/Palette checks
passed. BricsCAD reported `25.2.10`, x64 CLR `4.0.30319.42000`, native runtime
major 25 and the exact worktree assembly. Plugin/Core ProductVersion was
`0.1.0-preview.10081`; SourceLink was bound to the exact candidate. The
runtime plugin SHA-256 was
`443847228CD8176A42111CF668F451ED9BBA94456F6D48B417DB12A592CEA079`.

The fresh guarded Meter lifecycle used `INSUNITS=6` and one 5 m production
GlassWall source. It passed production Level/Curtain commands across two new
licensed V25 processes: Bottom `3 m + 0.1 m` and Top `7 m - 0.2 m` resolved to
`3.1000000000000005..6.8 m`; native/semantic Undo restored the Level config,
pre-build host and absent generated set; Redo restored coherent output; and
explicit QSDB/DWG save persisted before cold reopen. Reopen and
ownership-scoped rebuild retained stable `1 host / 10 frame / 15 panel`
counts, removed the old generated set, produced disjoint replacements and
reported zero Level/P11 Health issues. Both host processes exited gracefully
with exit code `0`; the final marker reported `level_lifecycle_qualified=true`.

The matching Core beside the loaded plugin had SHA-256
`ED7B792062F3D99AD091F0FB3E8BBA7B1DAAF6558F16561E0740306922805FAD`.
DemandLoad was guarded `2 -> 4 -> 2`; the installed Loader path and SHA-256
`0D89D8D828BCE5CFC966EC2EF54358DC50E4FED560D5A908F94643AFA1D74E30`
were unchanged. The disposable DWG changed on save/rebuild and was then
restored byte-for-byte to
`CEC1350FB2207542AEECD96A790A198A6C9CC9E99A9F875871F367554B3D967E`.
Sidecar, script, lock and backup cleanup passed, zero BricsCAD processes
remained, the tracked tree stayed clean, and a bounded delayed Application-log
audit found zero BricsCAD Application Error, WER, Application Hang or `.NET
Runtime` events.

Two earlier attempts remain raw ignored provenance and do not weaken or replace
the final result. The first passed all preflights but stopped before host launch
because the machine had runtimes but no .NET SDK. After installing official
.NET SDK `8.0.424` under ignored task artifacts, the second passed source/build
checks but correctly rejected an OnStartup-installed DLL identity. The final
run isolated OnStartup DemandLoad with the guarded command-trigger mode and
loaded the exact worktree binary. Both earlier attempts are
`LOCAL_ENVIRONMENT / NO PRODUCT VERDICT`.

## Implementation and evidence boundary

This is a LOCAL_ONLY runtime lane. Meter-only orchestration adjustments stay in
gitignored `artifacts/`; production source and tracked tests/runners are not
owned here. The run must pin exact Git/ProductVersion/SourceLink identity and
plugin/Core hashes, preserve DemandLoad and installed-loader bytes, restore the
repository fixture byte-for-byte, remove sidecar/script/lock/private state and
leave zero BricsCAD processes.

A reproducible product failure stops the run and creates a separate remote/source
child with the smallest sanitized classification. A harness-only failure may be
repaired only under ignored artifacts. No private/customer DWG, release, signing,
manual Actions operation or `main` merge is authorized.

## Non-overlap

- Do not rerun or relabel the already-PASS Millimeter Level lifecycle, focused
  mm/m Level probe, curved/round lifecycle, LOCAL-017/018/019, #1744, #3613,
  H.1 P07, LOCAL-004 P01-P06 or unchanged #3681.
- Do not claim complete-family, broader multi-DWG, private-DWG or
  `FULL_LOCAL_MATRIX_PASS` coverage from this representative Meter row.
- Keep LOCAL-003 and #72 open after a bounded PASS.

## Completed validation

- official exact-candidate aggregate/local baseline;
- Core Release plus deterministic smoke;
- V25 Release x64 and offline WPF;
- exact-candidate licensed NETLOAD/Ribbon/Palette/native-host identity;
- focused Level lifecycle, Curtain P11, product-boundary and CI-policy guards;
- licensed two-process Meter lifecycle plus cleanup audit.

This closes issue #3811 only. The marker intentionally retains
`production_local003_qualified=false`; complete-family dual-unit lifecycle,
broader multi-DWG and explicitly authorized private-DWG coverage remain
`PENDING_LOCAL`, so LOCAL-003 and parent #72 stay open.
