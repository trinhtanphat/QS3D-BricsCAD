# V25 draft rollback tag-preservation runbook

Issue: #5881  
Lane-Key: `issue-5881`

## Purpose

The V25 release rollback helper may delete only the exact transaction-owned draft release. It must preserve the exact release tag after cleanup, even when the current workflow originally created that tag.

The previous topology enumerated all releases, re-resolved the tag SHA, and then deleted the tag ref. A concurrent actor could attach/create a release against that reusable tag after enumeration but before the destructive ref DELETE, so the DELETE acted on state not atomically covered by the ownership observation.

## Required contract

1. Resolve the exact remote tag and require it to match `WorkflowSha` before rollback.
2. If `ReleaseId > 0`, re-fetch and validate exact release id, repository URL, draft state, and tag name before deleting that draft.
3. Reconcile ambiguous draft DELETE acknowledgement by authoritative GET/404 semantics.
4. Exhaustively enumerate release owners after draft cleanup and fail closed if any release owns the tag.
5. Re-resolve the exact tag after cleanup and require the same `WorkflowSha`.
6. Preserve the exact tag for retry. There is no tag-ref DELETE endpoint, reconciliation helper, or `TagDeleted = $true` success state.
7. Return truthful `TagCreatedByThisRun` provenance while reporting `TagDeleted = $false`.

## Deterministic validation

Run:

```text
python scripts/preflight-v25-rollback-tag-preservation.py
```

The auto-discovered guard checks the retained draft identity/deletion gates, exhaustive owner scan, post-cleanup exact-SHA resolution, preservation marker and non-destructive result. Mutation controls verify each required token is independently guarded and reject reintroduction of tag DELETE URI/reconciliation surfaces.

This is REMOTE_SAFE release infrastructure validation. It does not claim licensed BricsCAD runtime evidence or production release publication.
