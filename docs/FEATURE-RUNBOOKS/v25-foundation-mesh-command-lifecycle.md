# V25 Foundation Mesh command lifecycle

## Scope

`QS3DFOUNDATIONREBAR3D` is a V25 authoring command. This carrier makes selection admission, native geometry handoff, transaction outcome, and post-commit UI publication one coherent document-generation lifecycle.

## REMOTE_SAFE qualification

- `scripts/preflight-v25-foundation-mesh-command-lifecycle.py` pins one command-captured `ObjectId[]` snapshot through builder handoff.
- The command captures `document.Database.UnmanagedObject` and requires the same managed active document and native database generation immediately before mutation.
- The builder clones the admitted `ObjectId[]`; it does not call `SelectImplied`, `GetSelection`, or `SetImpliedSelection`.
- Pre-commit exceptions restore `ProjectStateSnapshot`; rollback failure preserves both operation and restore failures through `AggregateException`.
- Only `ObjectDisposedException` observed from native transaction/document-lock cleanup after a successful CAD commit may return a committed result with `PostCommitCleanupWarning=true`, preventing a false mutation failure and unsafe user retry. Other post-commit exceptions propagate and must not be relabeled as cleanup success.
- Post-commit refresh/Regen/status/editor publication revalidates the exact document/native database generation between each step and redacts host exception-derived details.
- Required repository source guards, deterministic smoke, Core build, and locked-reference BricsCAD V25 compile are REMOTE_SAFE evidence when CI is terminal GREEN on the exact candidate SHA.

## LOCAL_ONLY qualification

A real licensed BricsCAD V25 runtime is still required to claim native disposal timing, MDI/document replacement timing, PICKFIRST behavior in the host, and visible palette/editor behavior. Remote CI must never be reported as `LOCAL_PASS`.

## Regression scenarios

1. Capture a valid Foundation selection, then change current/PICKFIRST selection before builder entry: native geometry must still use the admitted snapshot only.
2. Switch active DWG or replace the native database generation after semantic admission: mutation must fail closed before geometry handoff.
3. Throw before `Transaction.Commit()`: project semantic state must restore; a restore failure must retain both exceptions.
4. Throw `ObjectDisposedException` from transaction/document-lock cleanup after commit: command must report committed success with a bounded cleanup warning and must not invite retry. A generic post-commit exception must propagate instead of masquerading as cleanup success.
5. Switch active document/native generation during post-commit UI synchronization: remaining palette/Regen/editor publication must stop without touching the new drawing.
6. UI synchronization failure must emit only bounded product text; raw host exception message/type is not public output.
