# Zone Manager failed-publication lifecycle

Canonical carrier: Issue #4967 / Lane-Key `issue-4967`.

Runtime disposition: `LOCAL_ONLY` for licensed native-window qualification. Hosted source guards, Core smoke and V25 compilation are repository evidence only; they are not licensed BricsCAD `LOCAL_PASS`.

## Product boundary

`QS3DZONES` owns one modeless Zone Manager candidate process-wide. The owner binds both the native BricsCAD database identity and the managed `Document` wrapper used by the WPF window. A manager may be reused only when both identities still match the active document.

The command must never create a replacement while a prior pending or published owner has not reached terminal close. This matters when host publication throws, returns a non-loaded window, close is vetoed/throws, a BricsCAD document wrapper drifts for the same native database, or an old `Closed` callback arrives after a newer owner exists.

## Required lifecycle

1. Drain `_pending` before examining or constructing a replacement. Pending cleanup has no stale-reference shortcut: terminal `Closed` must release exact pending ownership.
2. A loaded `_published` owner is reused only when native database identity and managed wrapper identity both match the active document.
3. A mismatched loaded published owner is closed before replacement; failed/non-terminal close refuses construction.
4. A non-loaded published owner may be repaired as stale only through exact published ownership.
5. Construct the window and owner, attach exact-instance `Closed`, then assign `_pending` before `Application.ShowModelessWindow`.
6. Host publication is accepted only when `window.IsLoaded` and `_pending` still references the exact owner.
7. Transfer `_pending -> _published` only after those checks.
8. If publication fails, attempt to close only the exact pending candidate. If close does not terminally release it, keep `_pending` so the next invocation drains/fails closed rather than constructing a duplicate.
9. `Closed` callbacks release only matching `_pending` / `_published` owners; stale callbacks cannot clear a newer instance.
10. Palette/editor failures expose a stable/type-only message and never raw host `Exception.Message` content.

## Deterministic source validation

Run from repository root:

```text
python scripts/preflight-zone-manager-publication.py
python scripts/preflight-zones.py
dotnet build src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj -c Release -p:Platform=x64
```

The shared protected PR lane additionally runs repository feature guards, deterministic Core smoke and locked-reference V25 compilation.

## Licensed BricsCAD V25 qualification matrix

Use the exact pushed candidate SHA and a disposable test profile/DWG set. Do not infer results from source inspection.

- **Z01 — same-DWG reuse:** invoke `QS3DZONES` repeatedly; exactly one loaded manager remains and subsequent calls activate it.
- **Z02 — cross-DWG replacement:** open manager in DWG A, activate DWG B, invoke again; A owner reaches terminal close before B manager publishes and only one manager remains.
- **Z03 — wrapper drift:** with the same native database represented by a changed managed `Document` wrapper, invocation must close/rebind instead of reusing the old wrapper-bound window. If close is vetoed/fails, no replacement is constructed.
- **Z04 — host-show exception / terminal cleanup:** inject or reproduce `ShowModelessWindow` failure where candidate close succeeds; retry publishes exactly one manager.
- **Z05 — host-show exception / failed cleanup:** inject show failure plus close throw/veto/non-terminal outcome; retry refuses replacement while exact pending owner remains. After terminal close, retry may publish one manager.
- **Z06 — non-loaded publication:** force host show to return while `IsLoaded == false`; behavior follows the same pending cleanup/fail-closed rules and never publishes the non-loaded candidate.
- **Z07 — stale callback isolation:** close an older candidate after a newer owner is established through a controlled lifecycle sequence; old `Closed` must not release the newer pending/published owner.
- **Z08 — document-bound mutation:** switch active documents while the manager is open and exercise supported Zone actions; no Zone mutation may leak into the wrong DWG.
- **Z09 — error redaction:** inject a unique sentinel into a host exception message; Palette/editor output must not contain the sentinel or raw host message.
- **Z10 — cleanup/persistence:** close manager, save/cold-reopen the DWG and invoke again; no orphan modeless owner or duplicate manager remains and existing Zone project semantics persist.

Record exact BricsCAD build, candidate SHA, fixture identity and per-cell evidence. Only a real licensed run may be labeled `LOCAL_PASS`.
