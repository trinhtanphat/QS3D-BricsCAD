# BricsCAD V25 Start Center refresh enqueue intent lifecycle

## Scope

Lane C03. This guard covers the modeless Start Center deferred refresh scheduler in `src/QS3D.BricsCAD.V25/UI/BltStartCenterWindow.cs`.

## Defect

`QueueHomeRefresh` publishes a non-`Preserve` active-drawing record intent before it attempts `Dispatcher.BeginInvoke`. If `BeginInvoke` throws during dispatcher shutdown or another enqueue failure, no drain owns that intent. Leaving the intent published allows a later successful refresh to consume stale `Record`/`Suppress` state after the host document generation has changed.

## Required invariant

A failed dispatcher enqueue is an abandoned refresh generation. The catch path must clear queue ownership first and then reset `_queuedActiveDrawingRecordIntent` to `Preserve`. It must not capture or retain a native `Document` wrapper while scheduling deferred UI work.

A successful already-queued drain keeps the existing coalescing semantics: later non-`Preserve` intents may update the queued intent and the single queued drain consumes the latest owned intent.

## Deterministic / remote validation

Run:

```text
python scripts/preflight-v25-start-center-refresh-enqueue-intent.py
```

The source guard verifies the `BeginInvoke -> catch -> queue clear -> intent clear` ordering and rejects native document capture inside `QueueHomeRefresh`.

The shared branch CI must additionally complete generic/discovered source guards, deterministic smoke, and the BricsCAD V25 build against admitted locked reference generations on the exact candidate head.

## Licensed local classification

A real injected WPF dispatcher enqueue failure during BricsCAD V25 document activation/destruction is `LOCAL_ONLY`. Do not claim `LOCAL_PASS` unless exercised in a licensed BricsCAD V25 runtime. Remote/static green and admitted-reference compile do not imply native runtime PASS.

## Self-review checklist

- window close abandons pending intent;
- failed enqueue abandons pending intent and queue ownership;
- successful queued drain still coalesces intents;
- no deferred native `Document` wrapper retention;
- subscribe/unsubscribe remains transactional/best-effort on teardown;
- no CAD transaction, command dispatch, project mutation, or geometry ownership is introduced by the cleanup;
- exception details remain unsurfaced to modeless UI.
