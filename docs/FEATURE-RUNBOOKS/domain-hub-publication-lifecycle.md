# Domain Hub modeless publication lifecycle

## Scope

`QS3DDOMAIN` is intentionally a host-global modeless command hub. `DomainHubWindow` does not retain a BricsCAD `Document`; each button resolves `Application.DocumentManager.MdiActiveDocument` at click time and dispatches to that active drawing. This package hardens launch/publication failure ownership only and does not change underlying domain command semantics.

## Proven defect on main

The previous source kept an unpublished candidate only in a local variable. If `Application.ShowModelessWindow(...)` threw, or returned with `IsLoaded == false`, `finally` attempted `Close()` and then discarded local ownership. If that close threw or the candidate remained loaded, the next command invocation had no authoritative pointer to the failed candidate and could construct another modeless Domain Hub. The catch path also surfaced raw host `ex.Message` text to the active editor.

## Source-ready contract

- At most one authoritative loaded publication exists in `_published`.
- A new candidate becomes `_pending` before modeless host publication.
- The exact pending owner remains authoritative across invocations until terminal close is proven or ownership transfers to `_published` after a successful loaded publication.
- Every invocation drains a prior `_pending` candidate before constructing another. If best-effort close leaves it loaded, the command fails closed and constructs no duplicate.
- `Closed` clears only matching pending/published ownership; stale callbacks cannot clear a newer owner.
- An already-loaded `_published` window is reused/activated.
- Static publication transfers only after `Application.ShowModelessWindow` returns and `IsLoaded` is true.
- User-visible failure status is stable/redacted. Diagnostics may write exception type, never raw host exception message.
- The window remains host-global and continues resolving the active document at click time rather than retaining a managed `Document`.

## Remote deterministic validation

Run on one exact pushed candidate:

```text
python scripts/preflight-domain-hub-publication.py
python scripts/preflight-domain-hub-publication-lifecycle.py
dotnet build src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj -c Release -p:Platform=x64
```

The focused and historical guards pin pending ownership, drain-before-construct ordering, loaded-only publication, exact-owner release, fail-closed close semantics, exception redaction, and the host-global active-document boundary. Remote/static/build evidence is never `LOCAL_PASS`.

## LOCAL_ONLY licensed V25 matrix

Use one exact source/plugin identity in licensed BricsCAD V25 and disposable drawings only. Record BricsCAD ProductVersion, exact source SHA, loaded DLL hash, drawing identity, and sanitized command trace.

1. **Normal publication/reuse** — invoke `QS3DDOMAIN` twice and prove exactly one Domain Hub remains live; the second invocation activates/reuses it.
2. **Normal close/reopen** — close the hub normally, invoke again, and prove exactly one replacement opens with no stale-owner effect.
3. **Active-document dispatch** — with drawing A active, execute representative hub commands; switch to drawing B without recreating the hub and prove later clicks target B.
4. **Show exception** — with an approved local harness/probe, force modeless host publication to throw. Prove no static publication occurs and the failed exact candidate remains pending-owned until terminally closed.
5. **Non-loaded show return** — force host show to return with `IsLoaded == false`; prove the candidate remains pending-owned until terminal close and is never published.
6. **Pending close failure** — force best-effort `Close()` to throw or leave the failed candidate loaded. Invoke `QS3DDOMAIN` again and prove it fails closed without constructing a second candidate.
7. **Pending recovery** — after the exact failed candidate is terminally closed, invoke again and prove one clean publication can proceed.
8. **Stale callback isolation** — after recovery/publication of a newer owner, trigger/observe terminal close of an older candidate and prove it cannot clear the newer authoritative owner.
9. **Error redaction** — inject a host exception containing a unique sentinel and prove user-visible editor/status output never contains the sentinel; exception type may be present in diagnostics.
10. **Cleanup** — close all hubs/drawings/host as required by the local harness and verify no owned modeless/process/private-state residue remains.

Report `LOCAL_PASS` only when this licensed-host matrix is actually executed against the exact candidate SHA. Otherwise report only remote/static/build evidence.
