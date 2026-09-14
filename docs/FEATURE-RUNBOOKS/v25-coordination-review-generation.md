# BricsCAD V25 Coordination review native-generation affinity

Issue: #6925  
Lane: C03 — BricsCAD V25 UI / Workspace / Modeless / Authoring  
Ownership-Key: `v25-coordination-review-transient-generation-v1`

## Failure mode

Coordination Manager review owns transient native presentation state: entity highlights, implied selection, object isolation mode/commands, and a captured editor view. Those values belong to one exact BricsCAD native `Database` generation.

A managed `Document` wrapper can remain reference-equal while its native database is reloaded or replaced. Managed-document equality alone therefore permits an ABA failure where state captured from generation A is restored or published into generation B.

## Production contract

`TransientReviewSession` captures the non-zero native database identity at construction. Native-generation freshness is intentionally separate from MDI activity: switching to another DWG does not mean the owner generation became stale.

The session revalidates the exact owner generation and active-document requirement at native access/re-entrant publication boundaries, including highlight cleanup, implied selection, `OBJECTISOLATIONMODE`, isolate/unisolate commands, section/focus view state, cleanup retry/disposal and status publication.

When the same managed wrapper now exposes another native database, generation-A ownership is **abandoned without native restoration**. Saved ObjectIds, isolation/selection rollback debt, system-variable baseline and view snapshot are dropped so none can be applied to generation B.

Ordinary same-generation MDI switching is different: pending cleanup ownership is retained while a foreign DWG is active. A failed isolation compensation keeps its implied-selection baseline and system-variable debt for retry when the owning DWG becomes active again. Disposal cannot mark the session complete while `HasTransientState` remains true.

## REMOTE_SAFE verification

Run:

```text
python scripts/preflight-v25-coordination-review-generation.py
```

Then require aggregate feature guards, deterministic smoke and BricsCAD V25 plugin build using admitted/trusted locked references on the final exact PR head. These are `REMOTE_SAFE` checks only.

## LOCAL_ONLY licensed BricsCAD matrix

Keep these `NO_RESULT` until executed in licensed BricsCAD V25:

1. Highlight, replace/reload the native database on the same managed `Document`, then Clear/selection-change/close. No generation-A ObjectId may be dereferenced in generation B.
2. Start isolation, switch MDI after implied-selection/system-variable mutation but before completion, then reactivate owner. Rollback debt must remain and retry; closing while foreign MDI is active must fail closed while debt exists.
3. Start isolation, replace the native database on the same wrapper, then restore/close/dispose. No `UNISOLATEOBJECTS`, implied-selection restore or system-variable write may reach generation B.
4. Apply Section / Focus, replace the native database before Restore View. The generation-A snapshot must be dropped without `SetCurrentView` into generation B.
5. Switch MDI with the original generation unchanged. Pre-deactivation cleanup/retry must remain functional and no application-level cleanup may target the foreign active document.
6. Trigger drift during highlight transaction, isolation dispatch, view write, or status publication. Later boundaries must fail closed and stale ownership must be abandoned.
7. Close/destroy the owner with transient state present. Disposal/subscription cleanup must remain idempotent and no native write may target another document.

Do not record `LOCAL_PASS` from remote/static CI or admitted-reference compilation.
