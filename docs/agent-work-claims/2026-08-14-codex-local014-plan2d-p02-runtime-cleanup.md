# Work claim — LOCAL-014/P02 licensed runtime cleanup and evidence

- Status: `COMPLETED`
- Agent: `codex-local-worker` (`/root`)
- Registered: `2026-08-14T13:08:00+07:00`
- Baseline main SHA: `05fa8363307557fb6b0d405ed566c283136c068f`
- Runtime candidate observed: `8fb008400aaed7581b349a66dd9496f1ce4f5a78`
- Priority: `LOCAL-014 / P1 / P02`

## Runtime evidence and diagnosis

The guarded licensed BricsCAD V25.2.10 x64 P02 run on exact clean SHA `8fb008400aaed7581b349a66dd9496f1ce4f5a78` reached a sanitized `status=PASS` product marker. Both production quick aliases converted one LINE and one open straight POLYLINE into two semantic `ArchitecturalWall` owners and two owned Solid3d outputs. Preferred Family values `0.31 m / 4.2 m / 0.45 m`, unrelated-dirty preservation, retained sources, disjoint ownership, native bounds and zero wall-scoped Core/runtime blocking Health counts all passed.

The PowerShell runner then rejected the otherwise successful run because BricsCAD had created the disposable drawing's ordinary native `.bak` before the runner's success-path private-state check. The existing `finally` cleanup removed that file, left zero BricsCAD processes, retained only the sanitized marker and restored the disposable DWG to its original SHA-256. This is a runner cleanup-order defect, not a Plan-to-3D production failure.

## Reserved scope

- `scripts/test-bricscad-v25-plan-to-3d-p02.ps1` — remove allowlisted disposable sidecar/backup/lock artifacts before asserting post-run cleanup, then verify they are absent.
- `scripts/preflight-plan-to-3d-p02-runtime-probe.py` — require the corrected cleanup order and preserve all privacy/exact-SHA/process/hash guards.
- `docs/LOCAL-AGENT-INBOX.md` and `docs/PLAN-TO-3D-WORKFLOW.md` — record only a successful exact-SHA rerun and its sanitized evidence.
- this claim file — claim-first registration and exact closeout evidence.

## Excluded scope

- No edits to `PlanTo3DCommands.cs`, probe C# commands, wall builders, Family/project/Health/ownership services or any production source.
- No expansion into ADV prompts, cancellation/drift, rollback injection, Undo/Redo, save/reopen, multi-DWG, LOCAL-002/003/004, P10/P11, V26, private/customer drawings or GitHub Actions.
- No overall LOCAL-014 promotion; P02 is one bounded positive slice only.

## Validation and completion

- Publish this claim to `origin/main` before implementation.
- Parse the runner, run the focused P02 gate, relevant Plan-to-3D gates, Core Release smoke and installed-reference V25 `Release|x64` build.
- Rerun the guarded P02 workflow on a fresh ordinary copy outside the repository, exact clean merged SHA, empty artifact directory and zero pre-existing BricsCAD processes.
- Require the same sanitized PASS marker, unchanged DWG hash, zero process, deleted script, absent `.qsdb`/`.bak`/lock state and reviewed privacy-safe metadata.
- Commit/push/merge only request-scoped changes; do not dispatch GitHub Actions.

## Closeout evidence

- Claim-only PR `#1136` squash-merged as `4c84a8f1075bbebd10990272676760fac60c35d4` before implementation.
- Runner/gate implementation PR `#1139` squash-merged as exact runtime candidate `7f57130470d4440f25dd27ea0bc3207cbb777a07`.
- Exact candidate validation: focused P02 gate PASS; generic preflight PASS; Core Release build `0` warnings / `0` errors and smoke `ALL PASS`; installed-reference V25 `Release|x64` build `0` warnings / `0` errors. Eight other Plan-to-3D gates passed; the finish-workflow aggregate gate had an unrelated current-main Ribbon literal drift and was not edited in this lane.
- Licensed P02 rerun PASS on BricsCAD V25.2.10 x64 with plugin SHA-256 `EE7FA1C5F1A28127622C76F9E246B2E1388E77ED1C2029E1167B7457EE336C80`.
- Sanitized marker: two quick aliases; one LINE plus one open straight POLYLINE; two semantic walls; two generated solids; preferred Family `0.31 m / 4.2 m / 0.45 m`; unrelated dirty and source geometry preserved; ownership disjoint; native bounds verified; wall-scoped Core/runtime Health error counts both zero.
- Disposable DWG SHA-256 stayed `CEC1350FB2207542AEECD96A790A198A6C9CC9E99A9F875871F367554B3D967E`; process, script and private drawing state cleanup were all verified. No private/customer data or GitHub Actions were used.
- P02 is qualified only for `P02_QUICK_ALIAS_POLYLINE_FAMILY_DIRTY_ONLY`; overall LOCAL-014 remains OPEN/PENDING_LOCAL for the excluded matrix.
