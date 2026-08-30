# Schedule Hub single-instance / wrapper-safe veto lifecycle

## Scope

`QS3DSCHEDULES` opens a document-bound modeless `ScheduleHubWindow`. The window intentionally retains its managed BricsCAD `Document` wrapper for activation checks, command dispatch, read-only snapshot refresh and `DocumentBoundWindowLifetime` ownership. The launcher therefore must not publish duplicate windows and must distinguish exact-wrapper reuse from native-database wrapper drift.

## Source-ready contract

- Publication records the exact `ScheduleHubWindow`, its exact managed `Document` wrapper, and a stable non-zero native database identity.
- Repeating `QS3DSCHEDULES` for the exact same native database and exact same managed wrapper reuses/activates the existing Schedule Hub.
- If the host presents a different managed wrapper for the same native database, the old wrapper-bound Schedule Hub is not reused. It must reach terminal close before a replacement is shown.
- Switching to another native database uses the same terminal-close arbitration.
- A close exception or close veto fails closed; no replacement is constructed or published while the prior owner remains loaded.
- A stale unloaded publication is cleared defensively.
- A new candidate is published only after `Application.ShowModelessWindow` returns and the candidate is still loaded.
- Only the matching candidate's terminal `Closed` callback may clear the published window/wrapper/native identity.
- Existing Schedule Hub behavior remains document-bound and read-only for snapshot generation; this source lane does not change schedule calculations or mutate project state.

## Hosted validation

Require Shared CI on one exact pushed candidate. Admission must pass Reservation/Lane-Key/path collision, generic source guard and all discovered feature source guards. Core must pass deterministic smoke tests, trusted BricsCAD V25 reference validation, V25 plugin compilation and final build.

The focused guard `scripts/preflight-schedule-hub-single-instance-veto-safe.py` rejects the historical unconditional new-window path and proves exact-owner reuse, terminal-close arbitration and candidate publication ordering.

## LOCAL_ONLY licensed matrix

On one exact source/plugin identity in licensed BricsCAD V25, using disposable drawings only:

1. Open drawing A and invoke `QS3DSCHEDULES`; prove exactly one Schedule Hub is visible and remains bound to A.
2. Invoke again with the exact same managed wrapper; prove the same window activates and no duplicate is created.
3. Exercise an authorized managed-wrapper refresh for the same native database where the host exposes one; prove the old wrapper-bound Schedule Hub reaches terminal close before a replacement is published.
4. With A's Schedule Hub open, activate drawing B and invoke `QS3DSCHEDULES`; prove A's owner reaches terminal close before B's hub is shown.
5. Exercise an authorized close-veto/close-failure probe; prove no second hub is published while the old window remains loaded, then retry after terminal close.
6. Close/destroy the owning drawing and verify the existing `DocumentBoundWindowLifetime` closes the hub and publication can be released.
7. Verify refresh and command buttons still require the correct active source drawing and no schedule snapshot path creates/replaces project state.
8. Reopen after terminal close and prove a fresh owner can be published exactly once.
9. Verify no owned modeless/process/private-state residue remains after cleanup.

Hosted static/compile evidence is never `LOCAL_PASS`; publish only sanitized exact-SHA/ProductVersion runtime evidence through the canonical local qualification flow.
