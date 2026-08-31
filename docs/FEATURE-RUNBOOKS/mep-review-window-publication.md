# MEP Review modeless publication lifecycle

Lane-Key: issue-4859

## Scope

`QS3DMEPREVIEW` is a host-global utility surface. It must not retain a BricsCAD `Document`, `ObjectId`, `DBObject`, or `Solid3d`; each command button continues to resolve `MdiActiveDocument` at click time. The window also edits the shared user recognition profile, so only a window that BricsCAD has actually loaded may become the authoritative owner.

The publication contract is:

`Closed handler -> ShowModelessWindow -> IsLoaded -> publish -> transfer cleanup ownership`

A loaded published owner is reused and activated. An unloaded/stale owner is released only by exact object identity. A candidate remains locally cleanup-owned until the host show call returns and WPF reports `IsLoaded`; show exceptions or non-loaded returns must leave no candidate in the authoritative slot. The exact-instance `Closed` callback releases only that published instance. Profile validation, XML hardening, atomic save/default fallback, command routing, and the existing read-only native boundary remain unchanged.

## Deterministic source validation

Run the auto-discovered guards:

- `scripts/preflight-cubicost-mep-review-workspace.py`
- `scripts/preflight-mep-review-window-publication.py`

The first retains the historical MEP workspace/profile/zoom safety checks and additionally rejects visibility-based or pre-show publication. The second pins exact loaded-publication ordering, exact release, cleanup ownership transfer, click-time active-document resolution, and the absence of retained native document/object fields.

Hosted Shared CI must pass exact-head `preflight` and `core`, including deterministic smoke, trusted BricsCAD V25 compile-reference validation, V25 plugin build, and final build. Hosted source/compile success is not licensed BricsCAD runtime evidence.

## LOCAL_ONLY licensed BricsCAD V25 matrix

Bind every result to one exact pushed source SHA, ProductVersion/plugin identity, BricsCAD version, and sanitized runner/probe identity. Start from a disposable/authorized profile state and restore it exactly afterward.

1. **Repeated invocation:** launch `QS3DMEPREVIEW`, then invoke it repeatedly while the first window is loaded. Prove one visible authoritative MEP Review window is reused/activated and no duplicate profile editor appears.
2. **Close/reopen:** close the authoritative window through the normal UI, invoke `QS3DMEPREVIEW` again, and prove a new loaded owner can be published without stale callbacks clearing it.
3. **Active-document switching:** with two disposable DWGs, keep the host-global MEP Review window open, switch the active DWG, invoke a safe review command from the window, and prove dispatch targets the current active document rather than a retained wrapper.
4. **Profile edit/save/reload:** make a bounded disposable recognition-rule edit, save, reload, verify the accepted profile and default/fail-closed validation semantics, then restore the exact pre-state. Repeated-window invocation must not create competing editors.
5. **Lifecycle cleanup:** close the window and host in the approved sequence; prove no C05-owned window/process/profile residue remains and exact pre-state is restored.
6. **Host-show exception/non-loaded boundary:** execute only when an evidence-backed licensed-host harness can safely and deterministically produce that boundary without patching production semantics. The candidate must not become authoritative and cleanup must complete. If the boundary cannot be induced with trustworthy evidence, record this row as `NO_RESULT`; do not simulate a runtime PASS from source inspection.

Expected successful native rows may be recorded as `LOCAL_PASS` only by the authorized licensed local execution path. Runtime failures are `RUNTIME_FAIL`; unobservable prerequisite/boundary rows are `NO_RESULT`. There is **no remote LOCAL_PASS** from this source carrier.

## Acceptance boundary

Source completion for issue-4859 requires production lifecycle correction, both deterministic guards, this prepared LOCAL_ONLY matrix, exact-head Shared CI, current-main collision-safe reconciliation, protected PR `preflight + core`, expected-head merge, and exact protected-main verification. Licensed runtime qualification remains separate and must never be inferred from merge or cloud CI.
