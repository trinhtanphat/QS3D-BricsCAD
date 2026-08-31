# Family + Level Manager failed-publication lifecycle

Canonical carrier: Issue #4999 / Lane-Key `issue-4999`.

Runtime disposition: repository-safe implementation and hosted validation are REMOTE_SAFE. Licensed BricsCAD native-window qualification remains LOCAL_ONLY and must be tied to one exact pushed SHA.

## Product contract

`QS3DFAMILIES` and `QS3DLEVELS` each own at most one modeless manager process-wide. Ownership is exact and document-bound by both native BricsCAD database identity and the managed `Document` wrapper.

For each manager:

1. Drain any `_pending` owner before inspecting or constructing a replacement.
2. Reuse a loaded `_published` owner only when native database identity and managed wrapper identity both match the active document.
3. Close a mismatched published owner before replacement; if terminal close cannot be proven, refuse replacement.
4. Construct the exact window/owner, attach exact-instance `Closed`, then assign `_pending` before `Application.ShowModelessWindow`.
5. Publication is accepted only when `window.IsLoaded` and `_pending` still references the exact owner.
6. Transfer `_pending -> _published` only after those proofs.
7. Failed host publication attempts close only the exact pending candidate. If cleanup is non-terminal, retain `_pending` so the next invocation drains/fails closed rather than constructing a duplicate.
8. `Closed` callbacks release only matching pending/published owners.
9. Palette/editor diagnostics expose stable/type-only failure information and never raw host `Exception.Message` content.

## Repository validation

Run:

```text
python scripts/preflight-family-level-manager-publication.py
python scripts/preflight-family-level-manager-single-instance-veto-safe.py
python scripts/preflight-document-bound-modeless-lifetime.py
dotnet build src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj -c Release -p:Platform=x64
```

The protected shared lane additionally runs all discovered feature guards, deterministic Core smoke and locked-reference V25 compilation.

## Licensed V25 qualification matrix

Use a disposable profile/DWG set on the exact candidate SHA. Execute both Family and Level variants for each applicable row.

- **FL01 same-DWG reuse:** repeated invocation leaves exactly one loaded manager and activates the existing owner.
- **FL02 cross-DWG replacement:** manager for DWG A reaches terminal close before a DWG B manager publishes.
- **FL03 managed-wrapper drift:** same native database with a changed managed wrapper is not silently reused; replacement is allowed only after terminal close.
- **FL04 host-show exception / successful cleanup:** retry after terminal cleanup publishes exactly one manager.
- **FL05 host-show exception / failed cleanup:** close throw/veto/non-terminal state retains pending ownership and retry refuses duplicate construction until terminal close.
- **FL06 non-loaded host publication:** a return with `IsLoaded == false` is never promoted to published ownership.
- **FL07 stale callback isolation:** a callback from an older candidate cannot clear a newer pending/published owner.
- **FL08 active-document mutation affinity:** Family/Level actions mutate only the active intended project/DWG and reject stale document affinity.
- **FL09 error redaction:** a unique host exception sentinel is absent from Palette/editor output.
- **FL10 save/cold-reopen cleanup:** closing, saving and reopening leaves no orphan owner and preserves existing semantic Family/Floor behavior.

Record exact BricsCAD build, plugin SHA, fixture identity and per-cell evidence. Hosted/static CI and source inspection are never `LOCAL_PASS`.
