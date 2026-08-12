# Project Save Identity Preflight Atomicity Plan

Date: 2026-08-11
Owner: ChatGPT Web / GPT-5.6 Sol
Claim: `docs/agent-work-claims/2026-08-11-2355-chatgpt-web-gpt56sol-project-save-identity-preflight-atomicity.md`
Status: `IMPLEMENTED_SOURCE_SIDE`

## Goal

Keep in-memory `ProjectState` drawing identity and `ChangeVersion` unchanged when `ProjectContextCoordinator.Save()` / Save As is rejected by a fail-fast no-write guard.

## Implemented behavior

`Save()` now resolves the canonical project and destination path first, performs the recovery-required overwrite block, preserves recovery metadata rollback, acquires `ProjectFileLock`, validates backing-store freshness/path-transition state, requires the sidecar baseline, and computes `pathTransition` **before** calling the mutating `SyncDrawingIdentity(project, document)`.

Only after those no-write guards pass does Save synchronize `DrawingPath` / drawing fingerprint / matching element fingerprints and `ChangeVersion`, immediately before the existing Store dispatch. Same-lock sidecar revision capture and `ProjectPersistenceStamp.MarkSaved()` ordering remain unchanged.

No whole-project rollback was added around Store I/O. A later Store failure may occur after publication/readback, so blindly reverting RAM identity could create disk/RAM divergence; this lane intentionally fixes deterministic pre-write rejection atomicity only.

## Verified dependency boundary

`EnsureBackingStoreUnchanged(document, project, true, ...)` remains independent of synchronized `project.DrawingPath`: it validates the cached project reference, baseline freshness, current destination path from `document.Name`, path-transition state, and destination sidecar existence.

## Regression gate

`scripts/preflight-project-save-identity-preflight-atomicity.py` verifies:

- path/recovery checks precede identity mutation;
- lock/freshness/baseline/pathTransition checks precede identity mutation;
- identity synchronization occurs exactly once and before every Store save branch;
- Store dispatches precede sidecar revision capture and `MarkSaved()`;
- recovery metadata restore remains in catch;
- target-sidecar overwrite refusal remains present;
- no broad `ProjectStateSnapshot`/persistence rollback was introduced across possibly-published I/O;
- `SyncDrawingIdentity` / legacy adoption remain state mutators, preserving the regression premise.

## Integration evidence

- Claim registration before source: `b81e277b75d714c6d4805d14623cfeb26a674cfe`.
- Planning before source: `29c01a21d60fc83adc39ebcc1ae4e6c198260e77`.
- Source commit: `94d5b9326e6084dc9541791fa8279cae37be721a` — exact diff 1 addition / 1 deletion, moving only the existing `SyncDrawingIdentity` call.
- Focused gate: `5c34ec2d9a156c66cf1c9016386b73a49c85c07b`.
- PR #548 raw pre-merge state: `mergeable=true`, `rebaseable=true`, `mergeable_state=clean`.
- Final merge: PR #548 -> `cb3dcfbd9dcafe6ceb37eb8692b6119462a2293f`, using expected head SHA and no force update.
- Merge-SHA source blob: `adc2e97885dff110734a997d6f5fb772115eb978`.
- Merge-SHA gate blob: `3b3f314cad1ee18dbb5867140f21d95de54cc7e1`.

## Qualification

- Current-main comparison before merge showed 23 intervening commits with no edit to `ProjectContextCoordinator.cs` or the focused gate.
- Merge-SHA source re-fetch confirms `pathTransition` is calculated before `SyncDrawingIdentity`, and all Store branches remain after it.
- Focused gate source was re-fetched and its ordering/source contracts inspected against the merge-SHA source.
- Six commits landed immediately after merge; none modified `ProjectContextCoordinator.cs` or the gate.
- GitHub registered no combined status checks and no workflow runs for merge SHA; absence is recorded, not treated as CI PASS.
- Historical `agent/local-v25-blt-parity-next-20260811` branch was verified 870 commits behind main with zero unique commits, so it did not own the edited file.

Interactive BricsCAD V25 Save As qualification remains `PENDING_LOCAL / DO_NOT_RETRY_REMOTE` under the existing local qualification queue. No native runtime PASS is claimed remotely.
