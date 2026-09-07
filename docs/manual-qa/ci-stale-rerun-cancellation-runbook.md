# CI stale-rerun cancellation qualification

Issue: #6012
Lane: C05
Ownership-Key: `ci.stale-rerun-cancellation-v1`

## Defect evidence

On PR #6011, current-head pull-request run `34090086154` for `94faa366a56e5b0138dd7f09d4423eccaae71d8b` started before a manual rerun of historical run `34089954971` for stale head `a35c3a172df4497f3d4f78bceec5377c840394ac`. The historical attempt shared the same PR branch/code concurrency key and cancelled the newer exact-head run at checkout. A later rerun of the current head then cancelled the stale attempt.

This is not push/PR cross-event collision: the same-head push run stayed in its distinct `push` concurrency class.

GitHub concurrency also replaces an existing pending run when a newer run enters the same group, independently of `cancel-in-progress`. Therefore conditional cancellation alone is insufficient: historical reruns must use a run-specific concurrency suffix so they cannot replace either running or pending live exact-head validation.

## Required invariant

- Preserve repository + head branch + event-class identity for ordinary first-attempt validation.
- First-attempt branch/PR events may cancel genuinely superseded live work in the same class.
- A manual rerun (`github.run_attempt > 1`) receives a `github.run_id`-specific suffix and must not share the live suffix used by current attempt-1 validation.
- Historical reruns must not cancel a running current head or replace a pending current head.
- Preserve canonical protected PR job names `preflight` / `core`, push names `branch-preflight` / `branch-core`, and edited metadata isolation.
- Do not reuse stale GREEN or weaken exact-head, reservation, source, smoke, or build gates.

## Qualification

1. `python scripts/preflight-ci-stale-rerun-cancellation.py`
2. `python scripts/preflight-all.py`
3. Fresh exact-head Shared CI on the carrier branch.
4. Confirm ordinary first-attempt push/PR validation still executes normally.
5. Hosted probe: while a newer exact-head PR-code run is active, rerun a historical carrier run whose workflow already contains this fix. Verify the stale attempt uses an isolated concurrency group and does not cancel/replace the current-head run.
6. Before merge, refresh protected main, require zero unresolved review threads and fresh exact-head required CI GREEN; merge only with expected-head SHA binding.
