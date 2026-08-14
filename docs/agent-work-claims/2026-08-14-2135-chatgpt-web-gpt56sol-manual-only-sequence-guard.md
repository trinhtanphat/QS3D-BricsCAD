# Work claim — manual-only preflight follows historical preview sequencing

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-review`
- Registered: `2026-08-14T21:35:00+07:00`
- Completed: `2026-08-14T21:52:00+07:00`
- Baseline main SHA: `92c8076a8362d86706c7b046c7eec70aa2ddc9d4`
- Claim commit: `a464fae71a14bc3b887f2f39f1eacf914b187e94`
- Implementation branch: `agent/chatgpt-web-gpt56sol-review/manual-only-sequence-guard-20260814`
- Implementation commit: `fa651b1f173815594a165c840b9748cc0004cb0b`
- Integration batch: `integration/chatgpt-web-gpt56sol-review-manual-only-sequence-guard-20260814`
- Integration merge: `197123caabd621641512fe428add7c05fc2bc748`
- Main landing PR: `#1356`
- Main landing SHA: `c20d72cdc6d4238c19d9e14c5af7c3655c064b0c`
- Trigger evidence: V25 cloud run `31810692054` (#188), job `94800326391`, failed only at `Manual-only CI policy gate` after release preparation had validated and prepared `v0.1.0-preview.10015`.
- Completion evidence: V25 cloud run `31811475106` (#190), job `94802885446`, passed `Manual-only CI policy gate` and `Generic source guard` on the post-integration source landing.

## Reserved scope

Repair the stale automatic-dispatcher assertions in `scripts/preflight-ci-manual-only.py` so the manual-only policy guard verifies the current history-derived preview sequence contract rather than requiring the removed `10000 + GITHUB_RUN_NUMBER` implementation.

## Expected surfaces

- `scripts/preflight-ci-manual-only.py` — replace obsolete literal-token requirements with fail-closed assertions that match the current dispatcher contract: published matching-series tag inspection, bounded `0..65535` ordinal handling, next-ordinal derivation, and dispatch of `release-v25-cloud.yml` with `confirm_release=RELEASE`.
- this claim only for lifecycle close-out.

## Collision boundaries

- Do not modify `.github/workflows/dispatch-v25-cloud-after-main-integration.yml`; the sequence migration already landed through PR #1352.
- Do not weaken manual-only/default workflow restrictions, main-only dispatcher guard, bot-push rejection, target-workflow restriction, or RELEASE confirmation.
- Do not touch feature source/runtime/native BricsCAD lanes.
- No no-op changes and no direct implementation commit to `main`.

## Validation result

- `scripts/preflight-ci-manual-only.py` now requires published matching-series tag inspection, bounded ordinals and `max + 1` derivation.
- It explicitly rejects `GITHUB_RUN_NUMBER` / `10000 +` public preview derivation.
- PR #1355 integrated the single-file implementation into the declared integration branch.
- PR #1356 landed that integration once on current `main` at `c20d72cdc6d4238c19d9e14c5af7c3655c064b0c`.
- Automatic dispatcher run `31811374516` succeeded and started fresh V25 cloud run #190.
- V25 #190 passed the previously failing `Manual-only CI policy gate` and the following generic guard, proving the deterministic blocker from #188/#189 is closed.

## Completion condition

Satisfied. The stale manual-only guard is reachable from canonical `main`, the automatic dispatcher remains history-derived, and fresh V25 #190 passes the repaired policy gate.
