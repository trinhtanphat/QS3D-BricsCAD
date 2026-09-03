# Model Health failure-surface redaction

Canonical carrier: Issue #5017 / Lane-Key `issue-5017`.

Runtime disposition: source/static checks and protected V25 compilation are REMOTE_SAFE. Licensed BricsCAD V25 UI/native qualification remains LOCAL_ONLY; remote CI or build success is not `LOCAL_PASS`.

## Product contract

Model Health is a document-bound, read-only diagnostic surface. Locate actions must remain behind active-DWG and immutable semantic-snapshot validation. Snapshot freshness verification must remain read-only and must never create/cache project state.

Two host/adapter exception paths are deliberately redacted:

- Locate callback/validation failures use one stable user message and never append the caught exception message.
- Freshness-check exceptions mark the snapshot stale with one stable reason and never append the caught exception message.

The redaction boundary must not weaken fail-closed behavior: a wrong active DWG or stale snapshot still blocks Locate; a freshness verification exception still disables the snapshot through `MarkSnapshotStale`; semantic stamps remain ProjectId, UpdatedUtc, ChangeVersion and DrawingFingerprint.

## Deterministic repository validation

Run:

```text
python scripts/preflight-model-health-error-redaction.py
python scripts/preflight-model-health-snapshot.py
```

Shared CI must also pass all auto-discovered feature guards, Core deterministic smoke tests and the V25 plugin build against locked BricsCAD reference generations for the exact candidate SHA.

## LOCAL_ONLY licensed V25 matrix

- MH01 normal current snapshot: launch Model Health and verify filtering/summary remains usable.
- MH02 normal Locate: selected issue locates successfully in the owning active DWG.
- MH03 wrong active DWG: switch documents before Locate; Locate is blocked and no CAD action runs.
- MH04 stale semantic project: mutate/reload project state; activation marks the snapshot stale and disables controls.
- MH05 Locate exception sentinel: inject/induce a locate callback failure whose exception text contains a unique sentinel; warning appears without the sentinel or raw host detail.
- MH06 freshness exception sentinel: induce a read-only freshness failure with a unique exception sentinel; stale banner appears without the sentinel or raw host detail.
- MH07 stale retry: after MH04/MH06, repeated Locate cannot bypass stale-state blocking.
- MH08 close/reopen: close the current window and open a fresh snapshot; one valid owner is visible and stale state does not bleed into the new instance.
- MH09 multi-DWG ownership: open from one DWG, switch among documents, and confirm the document-bound lifetime closes/invalidates safely without cross-DWG Locate.
- MH10 cold cleanup: close Model Health, close/reopen the drawing/process as appropriate, and verify no owned diagnostic window remains.

Record exact product SHA/artifact identity, BricsCAD build, scenario verdict and cleanup evidence. Only a licensed bounded host run may be reported as `LOCAL_PASS`.
