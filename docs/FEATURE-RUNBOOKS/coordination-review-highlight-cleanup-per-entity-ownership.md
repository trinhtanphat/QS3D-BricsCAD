# Coordination review per-entity highlight cleanup ownership

## Scope

This runbook qualifies the bounded source contract for `TransientReviewSession.ClearHighlight()` in the persisted Coordination Manager review UI. It does not claim licensed BricsCAD runtime acceptance.

## Defect boundary

A live cleanup attempt may successfully unhighlight some owned ObjectIds while another `GetObject` / `Unhighlight` call fails. Transaction commit alone is not sufficient evidence that every native highlight was cleared. Releasing the whole `_highlighted` set after such a partial attempt loses retry ownership for the failed native object.

## Required source contract

1. Snapshot the current persistent highlight ownership before cleanup.
2. Keep attempt-local `released` IDs and the first per-entity cleanup failure.
3. Do not mutate persistent ownership before the surrounding native transaction commits.
4. After commit, remove only IDs whose native unhighlight completed successfully.
5. Keep failed IDs owned so a later Clear/reset/Dispose attempt can retry them.
6. Surface incomplete live cleanup so retry-sensitive Dispose does not publish terminal disposal.
7. Preserve destroyed-document teardown as an explicit abandon path where native retry is impossible/unsafe.
8. A transaction-level failure publishes no per-entity ownership changes.

## Deterministic qualification

Run:

`python scripts/preflight-coordination-review-highlight-cleanup-per-entity-ownership.py`

The guard must be auto-discovered by aggregate feature guards and must reject whole-set live ownership clearing or release-before-commit source shapes.

## Runtime boundary

Hosted guards, Core smoke, trusted V25 compile references and V25 plugin compilation are source qualification only. No `LOCAL_PASS` is implied. If licensed runtime qualification is later requested, exercise a controlled per-entity cleanup failure/retry on an exact integrated SHA without fabricating native behavior.