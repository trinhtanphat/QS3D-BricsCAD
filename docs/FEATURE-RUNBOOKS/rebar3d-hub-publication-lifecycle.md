# Rebar 3D Hub publication lifecycle

## Source contract

`QS3DREBARHUB` is an application-wide modeless singleton. A loaded published owner is reused and activated. A stale unloaded owner is released only when it matches the authoritative `_window` slot.

A newly constructed `Rebar3DHubWindow` remains locally cleanup-owned until the host call completes and `IsLoaded` is true. Publication occurs only after `Application.ShowModelessWindow(...) -> IsLoaded`; immediately after `_window = window`, local cleanup ownership is cleared.

If host show throws or returns with `IsLoaded == false`, the candidate remains unpublished and the `finally` path best-effort closes it. The cleanup helper refuses to close the current authoritative published owner. `Closed` releases only the exact matching published instance.

## Remote validation

The auto-discovered `scripts/preflight-rebar3d-hub-single-instance.py` guard pins construction, exact `Closed` release, show/load/publication ordering, cleanup transfer, finally cleanup, and published-owner refusal. Shared CI also compiles the V25 plugin against trusted locked references. These checks are source/compile evidence only.

## LOCAL_ONLY V25 matrix

Run against the exact pushed candidate in licensed BricsCAD V25 Windows x64:

1. Normal open: invoke `QS3DREBARHUB`; confirm one loaded window and normal command behavior.
2. Reinvoke while loaded: confirm the same owner is activated and no duplicate window appears.
3. Terminal close then reopen: close the owner, invoke again, and confirm a fresh single owner appears.
4. Host-show exception probe: using an evidence-backed harness capable of inducing a `ShowModelessWindow` failure, confirm the unpublished candidate does not survive. If the host cannot deterministically induce the exception, record `NO_RESULT` rather than PASS.
5. Non-loaded return probe: using an evidence-backed harness capable of producing a non-loaded return, confirm the unpublished candidate is terminally closed and a later invocation can open normally. If not inducible, record `NO_RESULT`.
6. Successful publication survival: confirm cleanup transfer does not close the successfully published owner after `ShowModelessWindow -> IsLoaded`.

Do not infer or report `LOCAL_PASS` from hosted CI or static inspection.
