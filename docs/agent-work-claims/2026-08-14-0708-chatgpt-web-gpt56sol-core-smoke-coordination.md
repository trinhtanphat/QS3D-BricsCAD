# Work Claim — Core smoke coordination and triage

- Agent: `chatgpt-web-gpt56sol`
- Started: `2026-08-14 07:08 +07:00`
- Status: `ACTIVE`
- Current phase: `FIXING_RUN_139_SOURCE_LIVE_REGRESSION`
- Baseline observed before claim: `main` at `2dc87bf0985c5967f9ca45f09aac22ba85e2e0cd`
- Initial claim commit: `3cb24f5d9041209e00704d8270c61218278d8baf`
- Claim-amendment commit: `404e110d954a354541cefaf0c3dddde5e399c0e7`
- Source-fix commit now on `main`: `d03edf8e4c476ee929d731a2c0c7400a8b8d14e4`

## Scope

1. Record the current multi-agent coordination/blocker state in Markdown so other agents do not duplicate work.
2. Strengthen the claim-first rule for scope expansion: an agent must land and verify a follow-up claim-amendment commit on `origin/main` before touching any newly discovered source/test paths outside its reserved scope.
3. Triage the latest relevant deterministic Core smoke failure, obtain the exact exception/error text, and determine whether a remote-safe unclaimed source/test lane exists.
4. Resolve issue `#1092` while preserving canonical numeric SourceHandle identity and existing positive/negative smoke coverage.

## Initially reserved paths

- `docs/agent-work-claims/2026-08-14-0708-chatgpt-web-gpt56sol-core-smoke-coordination.md`
- `docs/AGENT-WORK-REGISTRATION.md`
- `docs/AGENT-CONCURRENCY-HANDOFF-2026-08-14.md`

## 2026-08-14 claim amendment — issue #1092

Fresh evidence after the initial claim identified the current CAD-independent blocker handed off by LOCAL-003:

- issue `#1092` is `OPEN`, unassigned and had no repository claim match for `1092` or `ModelHealthSourceHandleSmoke` at the collision check;
- clean exact SHA `2dc87bf0985c5967f9ca45f09aac22ba85e2e0cd` passes Core Release, nine focused Level/static gates and installed-reference V25 build, then full Core smoke fails in `ModelHealthSourceHandleSmoke.NumericAliasesShareIdentity`;
- numeric aliases `A` / `00a` produce zero `DUPLICATE_SOURCE_HANDLE` issues instead of one;
- the local owner explicitly handed this CAD-independent defect to a non-local owner and moved LOCAL-003 to `BLOCKED` without editing Core/tests.

After amendment commit `404e110d954a354541cefaf0c3dddde5e399c0e7` landed and was read back from `main`, this claim additionally reserved:

- `src/QS3D.Core/Diagnostics/ModelHealthService.cs`
- `tests/QS3D.Core.SmokeTests/ModelHealthSourceHandleSmoke.cs`
- issue `#1092` closeout/status updates tied only to this fix

The amendment does **not** reserve LOCAL-003 native BricsCAD qualification or any other source lane. If the work requires another source/test path, stop and land another claim amendment before reading/diagnosing/editing/testing that new path.

## 2026-08-14 read-only normalizer verification amendment

Before inspecting the implementation behind the newly used normalizer, this claim also reserves **read-only verification** of the symbol `GeneratedHandleIdentity` and the single source file that defines that symbol once located. The purpose is limited to confirming the exact numeric-alias and malformed-text fallback semantics required by #1092. This amendment does **not** authorize editing that helper. If a helper defect is found, stop and publish another claim amendment naming its exact path before any source modification or test expansion.

## 2026-08-14 07:31 +07:00 claim amendment — run #139 source-live split

Fresh workflow run `#139` (`31757404087`, job `94636105562`) on exact SHA `a2a1dcb1cbf4e5098a62d5707e25441f3ddf20be` passed feature guards, Core Release build and smoke-harness build, then failed deterministic Core smoke in `ComprehensiveGeneratedLiveHandleIdentitySmoke.SourceLiveSemanticsRemainTextual`: persisted source handle `0A` and live source handle `A` incorrectly matched, so expected `ORPHAN_HANDLE` was absent.

The existing smoke contracts intentionally distinguish two entry points:

- direct `ModelHealthService` keeps numeric SourceHandle alias identity for `A` / `00a` / `0xA`, including direct liveness matching required by `ModelHealthSourceHandleSmoke.NumericAliasesShareIdentity`;
- `ComprehensiveModelHealthService` keeps **source** live-handle semantics textual (`0A` must remain distinct from `A`) while generated-output live handles remain numeric-canonical.

This amendment additionally reserves diagnosis/testing and the minimal source fix for:

- `src/QS3D.Core/Diagnostics/ComprehensiveModelHealthService.cs`
- `tests/QS3D.Core.SmokeTests/ComprehensiveGeneratedLiveHandleIdentitySmoke.cs` (read/execute existing coverage; do not weaken expected behavior)
- the already-reserved `src/QS3D.Core/Diagnostics/ModelHealthService.cs` only as needed to expose an internal textual-source-liveness path without changing its public numeric-alias contract

No generated-handle normalizer semantics, LOCAL_ONLY runtime lane, or unrelated health provider is reserved by this amendment.

## Source fix currently on main

- `d03edf8e4c476ee929d731a2c0c7400a8b8d14e4` updates `ModelHealthService` so persisted and live source handles use the existing `GeneratedHandleIdentity.Normalize` identity contract.
- Numeric aliases such as `A`, `00a`, and `0xA` therefore canonicalize to one SourceHandle identity for intra-element duplicate detection, cross-element ownership detection and direct liveness matching.
- Malformed textual handles retain the existing trimmed, case-insensitive compatibility path because `GeneratedHandleIdentity.Normalize` falls back to trimmed text when hexadecimal parsing is not applicable.
- `liveGeneratedSolidHandles` intentionally remains on the pre-existing normalization path; this source-handle fix does not broaden generated-solid semantics.
- Run #139 proves this direct fix introduced a comprehensive-entry-point source-liveness regression that must be split without weakening either existing smoke contract.

## Concurrency incident during this claim

The exact source/test claim amendment `404e110d...` was visible before source work. While this lane was reserved, concurrent commit `d03edf8e...` landed directly on `main` in the same `ModelHealthService.cs` lane. A redundant recovery commit `4922d0c8287ea2427cedf3a4526daef4e73b2246` had already been prepared from the previously qualified source blob; PR `#1093` exposed only that one-file `+10/-2` recovery. Once the concurrent `d03edf8...` write was detected, PR #1093 was closed **unmerged** instead of stacking duplicate changes.

This incident is recorded in `docs/AGENT-CONCURRENCY-HANDOFF-2026-08-14.md` and motivated the stronger just-in-time exact-path collision rules in `docs/AGENT-WORK-REGISTRATION.md`.

## Collision rules for this claim

- Do not enter any source lane already marked `ACTIVE`/`BLOCKED` by another agent.
- In particular, re-check current ownership before touching Source Reconcile/#1005 or any LOCAL_ONLY BricsCAD-runtime lane; historical status is not sufficient.
- If `main` advances during investigation, refresh before any claim amendment or write.
- No speculative/no-op source commit and no CI-gate weakening merely to obtain green status.
- Immediately before source/test/script writes or PR merges, recheck exact-path commits/claims and stop rather than stack duplicate work.

## Evidence baseline

- `docs/AGENT-WORK-REGISTRATION.md` requires a claim-only commit on `origin/main` before implementation diagnosis/editing/testing and explicitly requires claim-only amendments before touching newly discovered surfaces.
- V25 workflow run `#139`, run id `31757404087`, job `94636105562`, exact SHA `a2a1dcb1cbf4e5098a62d5707e25441f3ddf20be`, fails only after Core build/harness success at deterministic Core smoke with missing `ORPHAN_HANDLE` in `SourceLiveSemanticsRemainTextual`.
- Earlier run #138 on stale SHA `93a5547224a5248ae741ccd8dd4368bac27b6b00` isolated the numeric-alias regression fixed by `d03edf8...` but is historical evidence only.
- A fresh workflow-dispatch run is required after the follow-up source fix; stale run reruns are not acceptable evidence for a newer SHA.

## Coordination/docs commits

- `36a0762c5e5565f19e2fb99e771e248213e982e8` — enforce claim amendments and just-in-time collision checks.
- `d06bfc8a41092d74f57ffbc5f93150ab03c1fc50` — shared 2026-08-14 Core-smoke/concurrency handoff.
- `aef15cf1a7a8294fcd2503683abeadc85b79f5b4` — correct stale V25 run number/SHA in shared handoff.

## Completion criteria

- Coordination/handoff Markdown committed and pushed to `main`.
- Claim-expansion/collision rules committed to `docs/AGENT-WORK-REGISTRATION.md`.
- Direct `ModelHealthSourceHandleSmoke.NumericAliasesShareIdentity` remains green without weakening numeric alias identity.
- `ComprehensiveGeneratedLiveHandleIdentitySmoke.SourceLiveSemanticsRemainTextual` is restored while generated-output live numeric aliases remain green.
- Fresh deterministic Core smoke on an exact current SHA passes both contracts; only then may #1092 be closed with exact evidence.
- LOCAL-003 remains untouched by this remote lane and must be reactivated by its local owner before native BricsCAD qualification resumes.
- Final canonical claim status becomes `COMPLETED` only after the evidence above; otherwise keep `ACTIVE` or use `BLOCKED` with exact reason.
