# Work claim — manual-only preflight follows historical preview sequencing

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-review`
- Registered: `2026-08-14T21:35:00+07:00`
- Baseline main SHA: `92c8076a8362d86706c7b046c7eec70aa2ddc9d4`
- Implementation branch: `agent/chatgpt-web-gpt56sol-review/manual-only-sequence-guard-20260814`
- Integration batch: `integration/chatgpt-web-gpt56sol-review-manual-only-sequence-guard-20260814`
- Trigger evidence: V25 cloud run `31810692054` (#188), job `94800326391`, failed only at `Manual-only CI policy gate` after release preparation had validated and prepared `v0.1.0-preview.10015`.

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

## Validation plan

- Run/require `scripts/preflight-ci-manual-only.py` through the repository's normal source-guard path.
- Ensure the preflight rejects run-number-derived preview sequencing and requires the current tag-history/bounds/next-ordinal dispatcher semantics.
- Integrate through the declared integration branch/PR, then follow the standing automatic dispatcher to a fresh V25 cloud run on exact post-integration `main`.

## Completion condition

The stale manual-only guard is updated on a reviewed agent/integration branch, the result is reachable from canonical `main`, and a fresh V25 cloud run passes `Manual-only CI policy gate`; then this claim can be marked `COMPLETED` with exact SHAs/evidence.
