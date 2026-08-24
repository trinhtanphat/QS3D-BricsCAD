# LOCAL-004 P04 Beam STRETCH dependent redistribution

Status: COMPLETED / LOCAL_PASS
Lane-Key: issue-3383
Parent: #80 / LOCAL-004
Carrier issue: #3383 (CLOSED)
Canonical PR: #3387 (MERGED)
Canonical branch: `agent/gpt56sol/issue-3383-beam-stretch-dependent-p04`
Baseline main: `afff082096998fa404f08a5e29bcfd9fbc3830dd`

## Source-prep delivered

This lane adds an automation-only exact-SHA licensed BricsCAD V25 qualification path for the next bounded native-edit cell after LOCAL-004 P03. It does not change production Source Reconcile, Beam, longitudinal-rebar or stirrup behavior.

The runner authors one production Beam from a 5 m LINE with the projectless `0.3 m x 0.5 m` profile, provisions the bounded fixture metadata `4D16` and `D8@1000`, then builds one host, four longitudinal bars and six stirrups through production commands.

One real top-level BricsCAD crossing-window `STRETCH` selects only the authoritative LINE endpoint and changes the source from 5 m to 8 m. Before reconcile, the probe requires the semantic state and every old host/rebar/stirrup output to remain at the 5 m baseline. Production `QS3DSYNCSOURCE` must then refresh the semantic/quantity state and remove the complete stale generated owner set. Explicit production rebuild must produce a disjoint 8 m host, four longitudinal bars and nine `D8@1000` stirrups.

The production planner arithmetic is pinned by the source guard: for 8 m, 25 mm cover and D8 stirrups, usable span is 7.942 m, eight intervals produce nine stirrups, and `GeneratedBeamStirrupActualSpacingM` must be `0.99275`. The longitudinal bars must span approximately `0.04 .. 7.96 m` along the Beam axis.

## Repository assets

- `src/QS3D.BricsCAD.V25/SourceReconcileNativeBeamStretchDependentRuntimeProbeCommands.cs`
- `scripts/test-bricscad-v25-source-reconcile-native-beam-stretch-dependent.ps1`
- `scripts/preflight-source-reconcile-native-beam-stretch-dependent-runtime-probe.py`

The runner requires interactive Windows, licensed BricsCAD V25 x64, the repository-generated disposable sample, an initialized V25 profile, an outside-repository empty artifact directory, a clean exact-SHA worktree and plugin/Core `ProductVersion` bound to that same SHA. It performs save/cold reopen and verifies process/script/private-state/drawing restoration before returning PASS.

## Source validation evidence

Source candidate `d91c49a85823e9a36fbae0f2e5bd4ec160344f76` passed automatic workflow #11868 / `32463924216`:

- `preflight`: SUCCESS, including PR admission, Lane-Key collision, generic guard, all discovered feature source guards, PowerShell syntax and V25 package integrity;
- `core`: SUCCESS, including Core build, deterministic smoke, trusted BricsCAD V25 reference validation and BricsCAD V25 plugin build.

This evidence proves source/static/compile readiness only. It is not licensed runtime evidence.

## Licensed local execution

The merged runner was executed from the clean exact source SHA recorded below with the x64 Release V25 plugin:

```powershell
pwsh -File .\scripts\test-bricscad-v25-source-reconcile-native-beam-stretch-dependent.ps1 `
  -BricsCadDir <licensed-V25-dir> `
  -PluginDll .\src\QS3D.BricsCAD.V25\bin\x64\Release\net48\QS3D.BricsCAD.V25.dll `
  -FixtureDwg .\samples\generated\QS3D-Sample.dwg `
  -Profile <initialized-test-profile> `
  -ArtifactDir <empty-outside-repo-dir> `
  -ConfirmDisposableCopies
```

A qualifying final marker must have:

- `qualification_boundary=LOCAL_004_P04_BEAM_DEPENDENT_STRETCH`
- `production_local004_p04_qualified=true`
- `native_stretch_verified=true`
- `pre_sync_output_isolation_verified=true`
- `source_reconcile_verified=true`
- `dependent_invalidation_verified=true`
- `dependent_rebuild_verified=true`
- `stirrup_redistribution_verified=true`
- `longitudinal_extent_verified=true`
- `cold_reopen_verified=true`
- `final_length_class=EIGHT_METERS`
- `stirrup_count_class=NINE_AT_D8_1000`
- `error_code=NONE`

Exact licensed evidence is `LOCAL_PASS` on source SHA `2985f13b0f0d680284e915fb81728bbb26a42ffe`, BricsCAD V25.2.10 and exact x64 plugin SHA-256 `C94A5ED5C8EA4CC039EE364B1DA021005AD07514F93BA60D091DFC88519C14E6`. The real crossing-window `STRETCH` changed the authoritative Beam LINE from 5 m to 8 m while semantic and generated state remained at 5 m before sync. Production reconcile invalidated the old host/four longitudinal bars/six stirrups; the explicit rebuild produced a disjoint 8 m host, four longitudinal replacements and nine `D8@1000` stirrups. Save, sidecar persistence, fresh-process cold reopen, drawing restoration and zero-process cleanup all passed with `error_code=NONE`.

This closes #3383/P04 only. Parent #80 remains open for the broader topology/category/dependent and failure/multi-DWG matrix.
