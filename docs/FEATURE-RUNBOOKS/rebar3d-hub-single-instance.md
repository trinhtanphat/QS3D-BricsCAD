# Rebar 3D Hub application-wide single-instance lifecycle

## Scope

`QS3DREBARHUB` is an application-level modeless command launcher. The window intentionally resolves `MdiActiveDocument` at button-click time, so it must remain usable while the user changes drawings. It must not accumulate duplicate live windows that can independently dispatch the same commands.

## Source-ready contract

- The launcher publishes at most one live `Rebar3DHubWindow` for the BricsCAD application process.
- Repeating `QS3DREBARHUB` while the published window is loaded reuses and activates that exact window; no candidate is constructed.
- Publication is not document-bound. Rebar commands continue to resolve the active drawing at click time through the existing window code-behind contract.
- A stale publication whose window is already unloaded is cleared before a new candidate is constructed.
- The candidate's exact `Closed` callback is attached before it is shown.
- A candidate is published only after `Application.ShowModelessWindow` returns and `IsLoaded` confirms the window remains live.
- Construction/show exceptions cannot overwrite the prior publication state.
- Only the matching published window's terminal `Closed` callback may clear `_window`; a delayed callback from an older candidate must not clear a newer owner.
- Existing command names, active-document dispatch, and Rebar Hub compile/application-bridge contracts remain unchanged.

## Hosted validation

Require Shared CI on one exact pushed candidate. Admission must pass Reservation/Lane-Key/path collision, generic source guard and all discovered feature source guards. Core must pass deterministic smoke tests, trusted BricsCAD V25 reference validation, V25 plugin compilation and final build.

The focused guard `scripts/preflight-rebar3d-hub-single-instance.py` proves live-owner reuse, stale-owner cleanup, construct/Closed/show/load/publish ordering, and exact-owner terminal release. Historical `preflight-rebar-hub-compile.py` must remain green.

## LOCAL_ONLY licensed matrix

Using a disposable licensed BricsCAD V25 session on one exact source/plugin identity:

1. Open drawing A and invoke `QS3DREBARHUB`; prove exactly one Rebar 3D Hub is visible.
2. Invoke `QS3DREBARHUB` repeatedly; prove the same window activates and no second live hub appears.
3. With the hub still open, activate drawing B and press representative read/write Rebar Hub commands; prove dispatch follows B at click time and does not remain bound to A.
4. Switch back to A and repeat a representative command; prove active-document dispatch follows A without recreating the hub.
5. Terminally close the hub, invoke again, and prove exactly one fresh owner is created.
6. Exercise an authorized host close/show-failure probe where available; prove a failed candidate is not published and a subsequent clean invocation can recover.
7. Close all disposable drawings/windows and verify no Rebar Hub/modeless/process/private-state residue remains.

Hosted source/build evidence is never `LOCAL_PASS`; publish only sanitized exact-SHA/ProductVersion runtime evidence through the canonical local qualification flow.
