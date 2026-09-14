# BricsCAD V25 Coordination review native-generation affinity

Issue: #6925  
Lane: C03 — BricsCAD V25 UI / Workspace / Modeless / Authoring  
Ownership-Key: `v25-coordination-review-transient-generation-v1`

## Failure mode

Coordination Manager review owns transient native presentation state: entity highlights, implied selection, object isolation mode/commands, and a captured editor view. Those values belong to one exact BricsCAD native `Database` generation.

A managed `Document` wrapper can remain reference-equal while its native database is reloaded or replaced. Managed-document equality alone therefore permits an ABA failure where state captured from generation A is restored or published into generation B.

## Production contract

`TransientReviewSession` captures the non-zero native database identity at construction and treats the pair of managed owner document plus native database identity as one immutable generation.

The session revalidates that generation before native access and at re-entrant/native publication boundaries, including:

- highlight and highlight cleanup transactions;
- implied-selection capture/restore;
- `OBJECTISOLATIONMODE` reads and writes;
- isolate/unisolate command dispatch;
- section/focus bounds and view capture/write/restore;
- cleanup retry and disposal;
- selection-change, activation/deactivation, window-close and status publication paths.

When the same managed wrapper is still present but the native identity no longer matches, generation-A ownership is **abandoned without native restoration**. Saved `ObjectId` values, isolation state, system-variable baseline and view snapshot are cleared from the session so none can be applied to generation B. Terminal document destruction uses the same no-write ownership release principle.

Ordinary MDI switching with the original native generation remains different from native-generation replacement: pre-deactivation cleanup may run while the owner generation is still active; foreign active documents never receive application/editor cleanup writes.

## REMOTE_SAFE verification

Run the focused source contract:

```text
python scripts/preflight-v25-coordination-review-generation.py
```

Then require the repository aggregate feature guards, deterministic smoke and BricsCAD V25 plugin build using admitted/trusted locked references on the final exact PR head. These are `REMOTE_SAFE` source/static/build checks only.

## LOCAL_ONLY licensed BricsCAD matrix

Keep these scenarios `NO_RESULT` until they are executed in a licensed BricsCAD V25 host:

1. Highlight a coordination issue, replace/reload the native database while preserving the same managed `Document` wrapper, then trigger Clear/selection change/close. Verify no stale `ObjectId` is dereferenced or unhighlighted in the successor generation.
2. Start isolation, capture a non-default `OBJECTISOLATIONMODE`, replace the native database on the same wrapper, then retry restore/close/dispose. Verify no `UNISOLATEOBJECTS`, implied-selection restore or system-variable write reaches the successor generation.
3. Apply Section / Focus, replace the native database before Restore View, then retry cleanup. Verify the generation-A view snapshot is dropped without a `SetCurrentView` into generation B.
4. Switch MDI documents with the original generation unchanged. Verify ordinary pre-deactivation cleanup remains functional and no application-level cleanup targets the foreign active document.
5. Trigger native-generation drift during highlight transaction, isolation command dispatch, section-view write, or status publication. Verify later boundaries fail closed and stale ownership is abandoned.
6. Close/destroy the owning document with transient state present. Verify disposal remains idempotent, subscriptions detach, and no cleanup write is sent to a different active document.

Do not record `LOCAL_PASS` from remote/static CI or admitted-reference compilation.
