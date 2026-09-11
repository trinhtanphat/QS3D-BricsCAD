# V25 report modeless document affinity

Issue: #6479
Lane: C03 — BricsCAD V25 UI / Workspace / Modeless / Authoring Workflows
Runtime: REMOTE_SAFE for source/static/admitted-reference compile; licensed host timing is LOCAL_ONLY / NO_RESULT.

## Scope

`QS3DFINISHSCHEDULE` and `QS3DWALLQTY` must bind each modeless window to the exact managed `Document` and native database generation that admitted it.

## Required lifecycle

1. Capture active managed document and non-zero native database identity.
2. Revalidate that exact generation before and after destructive close boundaries.
3. Root a candidate in `_pending` before `ShowModelessWindow` can pump host messages.
4. Capture an immutable owner in the `Closed` callback.
5. Revalidate exact document/native generation after host show.
6. Publish only when the window is Loaded and `_pending` still owns that candidate.
7. Retain pending/published ownership when close throws or is vetoed; never open a duplicate.
8. Suppress stale success/error UI after document-generation drift.

## Validation

Run `python scripts/preflight-v25-report-modeless-document-affinity.py`, document-bound/modeless admission guards, Reservation-v2 collision guard, `git diff --check`, and admitted V25 compile when Shared CI reaches that stage.

Real BricsCAD MDI A→B→A switching, close veto/failure, disposed-wrapper ABA and visible window behavior remain LOCAL_ONLY / NO_RESULT until executed on the licensed host.
