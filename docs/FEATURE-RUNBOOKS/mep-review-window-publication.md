# MEP Review modeless publication lifecycle

Historical Lane-Key: issue-4859
Current hardening Lane-Key: issue-4956

## Scope

`QS3DMEPREVIEW` is a host-global utility surface. It must not retain a BricsCAD `Document`, `ObjectId`, `DBObject`, or `Solid3d`; each command button continues to resolve `MdiActiveDocument` at click time. The window also edits the shared user recognition profile, so only a window that BricsCAD has actually loaded may become the authoritative published owner.

## Proven failed-publication defect

The prior source kept an unpublished candidate only in a local variable. If `ShowModelessWindow` threw, or returned with a candidate that did not remain loaded, `finally` attempted a best-effort `Close()` and then discarded local ownership. If close itself threw or left the candidate loaded, a later `QS3DMEPREVIEW` invocation could construct a second MEP Review/profile editor while the failed candidate was still alive. The launcher and click-time CAD dispatch also exposed raw host `ex.Message` text.

## Source-ready contract

- `_published` is the exact authoritative loaded MEP Review owner.
- `_pending` owns an unpublished candidate before host publication and remains authoritative across invocations until terminal close is proven or ownership transfers after successful loaded publication.
- Every invocation drains a prior pending candidate before constructing another. If best-effort close leaves the candidate loaded, launch fails closed and constructs no duplicate.
- A new candidate becomes pending-owned before attaching the exact-instance `Closed` callback and before `ShowModelessWindow`.
- Publication occurs only after `ShowModelessWindow -> IsLoaded`; then `_published` receives the exact window and pending ownership is released.
- `Closed` releases only matching pending/published ownership, so a stale callback cannot clear a newer owner.
- A loaded published owner is reused/activated. An unloaded published owner is released by exact identity before replacement.
- Launcher and click-time CAD-dispatch failures show stable/redacted messages; exception type may be retained, raw host exception message must not be surfaced.
- Profile validation, XML hardening, atomic save/default fallback, command routing, and the existing read-only native boundary remain unchanged.

## Deterministic source validation

Run the auto-discovered guards:

- `python scripts/preflight-cubicost-mep-review-workspace.py`
- `python scripts/preflight-mep-review-window-publication.py`

The historical workspace guard and focused publication guard both pin pending ownership, drain-before-construct ordering, exact-instance callback release, loaded-only publication, fail-closed pending cleanup, click-time active-document resolution, redacted host errors, and absence of retained native document/object fields.

Hosted Shared CI must pass exact-head `preflight` and `core`, including deterministic smoke, trusted BricsCAD V25 compile-reference validation, V25 plugin build, and final build. Hosted source/compile success is not licensed BricsCAD runtime evidence.

## LOCAL_ONLY licensed BricsCAD V25 matrix

Bind every result to one exact pushed source SHA, ProductVersion/plugin identity, BricsCAD version, and sanitized runner/probe identity. Start from a disposable/authorized profile state and restore it exactly afterward.

1. **Repeated invocation:** launch `QS3DMEPREVIEW`, then invoke it repeatedly while the first window is loaded. Prove exactly one authoritative MEP Review/profile editor is reused/activated.
2. **Close/reopen:** close the authoritative window normally, invoke again, and prove exactly one replacement can publish without stale-owner effects.
3. **Active-document switching:** keep the host-global window open across two disposable DWGs; click a safe review command before and after switching and prove each dispatch targets the active document at click time.
4. **Profile edit/save/reload:** make a bounded disposable rule edit, save, reload, verify accepted profile/default fail-closed semantics, then restore the exact pre-state.
5. **Show exception:** with an approved local harness/probe, force host modeless publication to throw. Prove no published owner appears and the exact failed candidate remains pending-owned until terminal close.
6. **Non-loaded show:** force host show to return with `IsLoaded == false`; prove the candidate is never published and remains pending-owned until terminal close.
7. **Pending close failure:** force best-effort `Close()` to throw or leave the failed candidate loaded; invoke `QS3DMEPREVIEW` again and prove it fails closed without constructing a second candidate.
8. **Pending recovery:** after terminally closing the exact pending candidate, invoke again and prove one clean publication proceeds.
9. **Stale callback isolation:** after recovery/publication of a newer owner, trigger terminal close of an older candidate and prove it cannot clear the newer owner.
10. **Error redaction:** inject a host exception containing a unique sentinel; prove editor and `_hostStatus` output never contains that sentinel. Exception type may be present.
11. **Cleanup:** close window/drawings/host in the approved sequence; prove no C05-owned modeless/process/profile residue remains and exact pre-state is restored.

Expected native rows may be recorded as `LOCAL_PASS` only by the authorized licensed local execution path. Runtime failures are `RUNTIME_FAIL`; unobservable prerequisite/boundary rows are `NO_RESULT`. There is **no remote LOCAL_PASS** from this source carrier.

## Acceptance boundary

Source completion for issue-4956 requires production lifecycle correction, both deterministic guards, this prepared LOCAL_ONLY matrix, exact-head Shared CI, current-main collision-safe reconciliation, protected PR `preflight + core`, expected-head merge, and exact protected-main verification. Licensed runtime qualification remains separate and must never be inferred from merge or cloud CI.
