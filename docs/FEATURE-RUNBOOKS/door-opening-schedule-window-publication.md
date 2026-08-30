# Door/Opening Schedule window publication lifecycle

Issue: #4895  
Lane-Key: `issue-4895`

## Source contract

`QS3DDOORSCHEDULE` owns at most one authoritative `DoorOpeningScheduleWindow` publication. Ownership is keyed by both the exact managed BricsCAD `Document` wrapper and a non-zero native database identity.

- A loaded owner for the same exact document/native database is reused and activated.
- A stale unloaded owner is released exactly.
- Wrapper drift for the same native database, or a different drawing, requires terminal close of the prior owner before replacement.
- Close exception or close veto fails closed: no second modeless owner is published.
- A new candidate remains locally cleanup-owned through `ShowModelessWindow` and the `IsLoaded` check.
- Publication records window, managed document, and native database identity before local cleanup ownership is cleared.
- A failed show or non-loaded return best-effort closes only the still-unpublished candidate.
- `Closed` releases state only when the callback belongs to the exact authoritative owner.

Hosted CI and locked V25 compile validate source/adapter behavior only. They do not constitute licensed BricsCAD runtime evidence.

## Deterministic repository validation

Run:

```text
python scripts/preflight-door-opening-schedule-window-publication.py
python scripts/preflight-door-opening-schedule.py
```

The auto-discovered publication guard pins exact lifecycle ordering and rejects the former direct `new DoorOpeningScheduleWindow(document)` show shape.

## LOCAL_ONLY licensed BricsCAD V25 matrix

Use an exclusive licensed V25 host and an exact package built from the candidate SHA. Record host version, package/source SHA, and cleanup evidence. Do not promote this runbook to `LOCAL_PASS` without executing every applicable row.

1. Open drawing A with a valid QS3D project and run `QS3DDOORSCHEDULE`. Confirm exactly one window appears and loads rows for A.
2. Run `QS3DDOORSCHEDULE` again without changing document. Confirm the same window is activated; no second top-level schedule window exists.
3. With A's window open, activate drawing B and run the command. Confirm A's owner terminally closes before one B-bound schedule is shown.
4. Return to A and repeat. Confirm B terminally closes before the A replacement; refresh/export continue to use A only.
5. Exercise document close/reopen or another supported wrapper-recreation path for the same native drawing identity. Confirm an old wrapper is never reused as the authoritative owner.
6. Close the schedule manually and invoke again. Confirm the exact `Closed` callback released ownership and one fresh window can be published.
7. During a controlled diagnostic build or host probe, force candidate show to return non-loaded/throw before publication. Confirm no orphan schedule window remains and the next normal invocation can publish one owner.
8. During a controlled close-veto/close-failure probe, request cross-document replacement. Confirm the command fails closed and does not publish a second owner until the previous window actually reaches terminal Closed.
9. Verify filtering, Refresh, project affinity, XLSX export, status text, and document-bound lifetime behavior remain unchanged for the active owner.
10. End with all schedule windows closed, no unexpected BricsCAD process ownership, and sanitized evidence attached to the local qualification record.
