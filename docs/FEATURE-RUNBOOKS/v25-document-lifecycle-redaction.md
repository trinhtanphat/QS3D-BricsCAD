# V25 document lifecycle publication redaction

## Classification

- Lane: C03 BricsCAD V25 UI / Workspace / modeless lifecycle.
- Remote/static contract: `REMOTE_SAFE`.
- V25 compile: admissible when the repository's hosted V25 compile gate has the required references.
- Licensed BricsCAD save/close/MDI failure injection: `LOCAL_ONLY / NO_RESULT` unless an exact licensed run is actually recorded.

## Problem

`DocumentLifecycleCoordinator` is a user-visible boundary: it writes to the active document editor, Workspace status, and close/save dialogs. Raw `Exception.Message` from filesystem, parser, host, database, or project services must not cross that boundary. Redaction must not erase lifecycle truth: a DWG SaveComplete event means the DWG save already happened; a failed sidecar save is post-commit work, while a failed sidecar save during BeginDocumentClose must keep the drawing open by vetoing close.

## Required invariants

1. No user-visible lifecycle reporting path derives text from `Exception.Message`.
2. Post-DWG-save sidecar failure explicitly states that the DWG save completed and the QS3D sidecar did not.
3. Begin-close sidecar failure keeps `e.Veto()` fail-closed and reports that the drawing remains open.
4. Recovery-copy success/failure remains distinguishable without exposing raw internal detail.
5. Project-load failures use stable redacted status while preserving revision-gated failed-reconcile memoization.
6. Document teardown remains independently fail-soft for every coordinator and reports only bounded classified counts, never raw exception text.
7. No redaction change may alter subscription teardown, queued lifecycle reconciliation, MDI document affinity, no-document reset, or persistence mutation ordering.

## Remote validation

```bash
python scripts/preflight-v25-document-lifecycle-redaction.py
```

Then run repository aggregate preflight/core gates and the admitted V25 compile gate on the exact PR head.

## Licensed runtime boundary

A complete local receipt, if executed, should inject representative failures for sidecar save after DWG save, BeginDocumentClose sidecar save, recovery copy, project load/reconcile, document activation/switch, and destroy teardown. Capture exact product/package/source SHA and prove UI text is redacted, close/save truth is correct, no stale document is mutated, and teardown leaves no duplicate subscriptions. Without that receipt, report `LOCAL_ONLY / NO_RESULT` rather than PASS.
