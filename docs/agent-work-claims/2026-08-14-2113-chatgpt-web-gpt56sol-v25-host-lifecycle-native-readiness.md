# Work claim — BricsCAD V25 host lifecycle and native dependency readiness

- Status: `BLOCKED`
- Agent: `chatgpt-web-gpt56sol-v25-native-readiness`
- Registered: `2026-08-14T21:13:00+07:00`
- Blocked: `2026-08-14T21:24:00+07:00`
- Refreshed blocker: `2026-08-14T21:46:00+07:00`
- Baseline main SHA: `6826ef6616e4d818ee377d2e4e581a75af27bd2c`
- Claim commit: `ab4c8ed9a5ef630d5aae36ad66e704f9be463b0f`
- Implementation branch: `agent/chatgpt-web-gpt56sol-v25-native-readiness/v25-host-lifecycle-native-readiness-20260814`
- Implementation commit: `6ded8225b895bb79de87545faff5261db64ac76d`
- Clean integration branch: `integration/chatgpt-web-gpt56sol-v25-native-readiness-20260814-r2`
- Clean integration commit: `e676ad900f8a03cb27201c2356255e03edb9410d`
- Clean integration PR: `#1354`
- Clean integration base SHA: `07335185bb5385eba49f21d16b24f89a73ee2083`
- Superseded PR: `#1348` (closed unmerged; no force-push)
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

- Do not modify V25 preview tag sequencing, release dispatcher/version scripts, `scripts/preflight-ci-manual-only.py`, or other surfaces reserved by the active release-preview-sequence claim.
- Do not modify Core persistence/project mutation surfaces reserved by concurrent claims.
- Do not redesign the updater protocol, release manifest, signing policy, Ribbon feature set, Palette UI, native geometry builders, or semantic model.
- Do not claim real `NETLOAD`, licensed BricsCAD V25, private-DWG, native UI, installer, or Authenticode `LOCAL_PASS` from remote/static evidence.
- Keep V26 build compatibility: V25-only BREP readiness checks must not introduce an unconditional V26 compile-time dependency on `TD_MgdBrep.dll`.

## Evidence / reason

Original source review found two release-grade adapter gaps: top-level host teardown could be stranded by one failing updater/native service, and `QS3DRUNTIMECHECK` validated BrxMgd/TD_Mgd while omitting the required V25 `TD_MgdBrep` assembly consumed by quantity geometry explanation.

Implementation `6ded8225b895bb79de87545faff5261db64ac76d` closes those source gaps. The first integration PR #1348 was later closed unmerged because a history refresh absorbed unrelated already-landed `main` commits and made the PR patch non-scope-clean. No force-push was used. Clean replacement branch `integration/chatgpt-web-gpt56sol-v25-native-readiness-20260814-r2` was built directly from main `07335185bb5385eba49f21d16b24f89a73ee2083`; exact compare to `e676ad900f8a03cb27201c2356255e03edb9410d` contains only the three reserved V25 source files plus `scripts/preflight-v25-host-lifecycle-native-readiness.py`. PR #1354 is the canonical integration PR.

## Current blocker

The original unsafe preview-number generator is no longer the blocker. Release-preview-sequence integration `e131b868292ccf6856af0287763bf52983a4d288` landed and the current dispatcher derives the next public preview from published matching-series Git tags instead of `GITHUB_RUN_NUMBER`. The release bot subsequently prepared the correct historical next ordinal `v0.1.0-preview.10015` on commit `92c8076a8362d86706c7b046c7eec70aa2ddc9d4`.

However V25 cloud run #188 / `31810692054` failed deterministically at `Manual-only CI policy gate` immediately after release-source preparation. `scripts/preflight-ci-manual-only.py` still requires the removed legacy dispatcher token `10000 + GITHUB_RUN_NUMBER`, so the policy guard rejects the newly-correct dispatcher before generic/feature guards or builds run.

That stale policy surface belongs to the still-ACTIVE release-preview-sequence ownership boundary. Current main commit `a464fae71a14bc3b887f2f39f1eacf914b187e94` is the other agent's claim-only reservation `claim: repair stale manual-only release sequence guard`. This lane must not collide with that work.

Unblock condition: the release-preview-sequence owner lands the stale manual-only policy repair, closes its claim as `COMPLETED`, and the known deterministic `Manual-only CI policy gate` blocker is absent on a fresh current-main V25 qualification path. Then refresh PR #1354 once more on the exact latest main and merge only its four reserved surfaces.

## Validation plan

- Deterministic regression/source guard is present for rollback-safe updater bootstrap, independent top-level teardown, V25 BREP runtime-major readiness and the V26 no-BREP shared-source boundary.
- `scripts/preflight-all.py` automatically discovers the new `preflight-*.py` guard in standard CI/release gates.
- Exact clean integration compare at `07335185... -> e676ad90...` contains only four reserved files.
- After the release-policy blocker is cleared, refresh current `main`, rebuild a scope-clean integration head if needed, merge once, then use the repository's standing exact-main V25 cloud CI path.
- Report source/static/CI evidence separately from licensed BricsCAD runtime evidence.

## Completion condition

V25 adapter startup/update lifecycle is rollback-safe, termination continues cleanup across independent host services, `QS3DRUNTIMECHECK` includes the required V25 BREP dependency without breaking the V26 shared-source boundary, deterministic regression coverage is present, the coherent implementation is integrated through the declared branch flow onto current `main`, and this claim is marked `COMPLETED` with exact implementation/integration/main SHAs. Licensed V25 runtime remains a separate LOCAL_ONLY qualification unless exact local evidence is supplied.
