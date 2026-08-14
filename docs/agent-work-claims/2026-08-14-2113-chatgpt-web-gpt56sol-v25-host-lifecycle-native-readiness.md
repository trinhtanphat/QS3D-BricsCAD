# Work claim — BricsCAD V25 host lifecycle and native dependency readiness

- Status: `BLOCKED`
- Agent: `chatgpt-web-gpt56sol-v25-native-readiness`
- Registered: `2026-08-14T21:13:00+07:00`
- Blocked: `2026-08-14T21:24:00+07:00`
- Baseline main SHA: `6826ef6616e4d818ee377d2e4e581a75af27bd2c`
- Claim commit: `ab4c8ed9a5ef630d5aae36ad66e704f9be463b0f`
- Implementation branch: `agent/chatgpt-web-gpt56sol-v25-native-readiness/v25-host-lifecycle-native-readiness-20260814`
- Implementation commit: `6ded8225b895bb79de87545faff5261db64ac76d`
- Integration batch: `integration/chatgpt-web-gpt56sol-v25-native-readiness-20260814`
- Integration commit: `5c7552e46d4df044481cdc6970c7171e576f2284`
- Integration PR: `#1348`
- Integration base SHA: `ff9c439b78caaba434a479df9a4e11d91d90f977`
- Priority: close remote-safe adapter/native-host safety gaps before treating the V25 adapter source lane as release-grade.

## Reserved scope

Harden the BricsCAD V25 plugin host lifecycle so an optional updater/bootstrap teardown failure cannot strand lifecycle/ribbon/palette cleanup, and make runtime readiness cover the native BREP dependency used by quantity geometry explanation rather than validating only BrxMgd/TD_Mgd.

The implementation must preserve the current V25 `net48/x64` hosted-plugin product boundary, preserve V26 source sharing, and fail closed on a mismatched/unavailable required V25 native dependency without manufacturing licensed runtime evidence.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/PluginEntry.cs` — contain top-level initialization/termination failure boundaries so partial startup is rolled back and teardown continues across independent services.
- `src/QS3D.BricsCAD.V25/Updates/UpdateBootstrapper.cs` — make start/stop state transitions rollback-safe/idempotent when update coordinator/event/window operations fail.
- `src/QS3D.BricsCAD.V25/RuntimeDiagnosticsCommands.cs` — include required V25 BREP assembly identity/readiness in `QS3DRUNTIMECHECK` while preserving the V26 shared-source build boundary.
- `scripts/preflight-v25-host-lifecycle-native-readiness.py` — deterministic source guard discovered automatically by `scripts/preflight-all.py`.
- this claim for implementation/integration close-out.

## Exclusions / collision boundaries

- Do not modify V25 preview tag sequencing, release dispatcher/version scripts, or other surfaces reserved by the active release-preview-sequence claim.
- Do not modify Core persistence/project mutation surfaces reserved by concurrent persistence claims.
- Do not redesign the updater protocol, release manifest, signing policy, Ribbon feature set, Palette UI, native geometry builders, or semantic model.
- Do not claim real `NETLOAD`, licensed BricsCAD V25, private-DWG, native UI, installer, or Authenticode `LOCAL_PASS` from remote/static evidence.
- Keep V26 build compatibility: V25-only BREP readiness checks must not introduce an unconditional V26 compile-time dependency on `TD_MgdBrep.dll`.

## Evidence / reason

Current `PluginEntry.Terminate()` executes updater, ribbon, document, palette and augmenter cleanup sequentially without a top-level containment boundary. `UpdateBootstrapper.Start()` sets `_started = true` before event subscription/coordinator startup and has no rollback if a later step throws; `Stop()` can likewise stop before completing unsubscribe/coordinator/window cleanup. By contrast, `DocumentLifecycleCoordinator` already rolls back failed startup and performs best-effort independent teardown.

`QS3D.BricsCAD.V25.csproj` requires `BrxMgd.dll`, `TD_Mgd.dll` and `TD_MgdBrep.dll`, and quantity geometry explanation consumes `Teigha.BoundaryRepresentation`; current `QS3DRUNTIMECHECK` validates only the BrxMgd and TD_Mgd runtime majors. That leaves the required BREP native-managed dependency outside the adapter readiness verdict.

Implementation `6ded8225b895bb79de87545faff5261db64ac76d` closes these source gaps. The refreshed integration commit `5c7552e46d4df044481cdc6970c7171e576f2284` was rebuilt on then-current main `ff9c439b78caaba434a479df9a4e11d91d90f977`; compare shows only the three reserved V25 source files plus the new preflight. PR #1348 is mergeable.

## Current blocker

Do **not** land PR #1348 while `docs/agent-work-claims/2026-08-14-2053-gpt56sol-release-preview-sequence-migration.md` remains `ACTIVE` and current `.github/workflows/dispatch-v25-cloud-after-main-integration.yml` still derives the public preview ordinal from `10000 + GITHUB_RUN_NUMBER`.

Any `src/**` push to `main` automatically triggers that dispatcher. During this work, the existing dispatcher already produced bot commit `ff9c439b78caaba434a479df9a4e11d91d90f977` (`chore(release): prepare v0.1.0-preview.10019`). Merging this source PR before the reserved dispatcher repair lands would knowingly create another release-side-effect race and would violate the active claim boundary.

Unblock condition: the release-preview-sequence owner marks that claim `COMPLETED` (or owner explicitly coordinates takeover), current `main` no longer derives public preview ordinals from `GITHUB_RUN_NUMBER`, then refresh/rebase PR #1348 on that exact main before merge.

## Validation plan

- Deterministic regression/source guard is present for rollback-safe updater bootstrap, independent top-level teardown, V25 BREP runtime-major readiness and the V26 no-BREP shared-source boundary.
- `scripts/preflight-all.py` automatically discovers the new `preflight-*.py` guard in standard CI/release gates.
- Refresh current `main` after the release dispatcher blocker is cleared, reconcile only non-overlapping changes, merge PR #1348 once, then use the repository's standing exact-main V25 cloud CI path.
- Report source/static/CI evidence separately from licensed BricsCAD runtime evidence.

## Completion condition

V25 adapter startup/update lifecycle is rollback-safe, termination continues cleanup across independent host services, `QS3DRUNTIMECHECK` includes the required V25 BREP dependency without breaking the V26 shared-source boundary, deterministic regression coverage is present, the coherent implementation is integrated through the declared branch flow onto current `main`, and this claim is marked `COMPLETED` with exact implementation/integration/main SHAs. Licensed V25 runtime remains a separate LOCAL_ONLY qualification unless exact local evidence is supplied.
