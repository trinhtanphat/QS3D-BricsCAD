# V25 project-persistence retryable native detach

## Scope

Issue #6366 hardens the BricsCAD V25 document-lifecycle ownership for the native `Database.SaveComplete` and `Document.BeginDocumentClose` subscriptions used to persist the QS3D sidecar and protect unsaved semantic changes during document close.

This carrier is intentionally limited to `DocumentLifecycleCoordinator` subscription/lifecycle behavior. It does not change MCP runtime/transport, installer/release, Quantity arithmetic, CAD geometry, or Core persistence semantics.

## Defect

The previous attachment sequence registered `Database.SaveComplete` before `Document.BeginDocumentClose`, but only published the managed handler dictionaries after both native add accessors completed. A native add can fail after partially registering a delegate. If the second add failed and compensation of the first registration also failed, managed ownership of the retained callback was lost.

The previous detach path likewise swallowed native remove failures and then removed the handler dictionaries unconditionally. BricsCAD could therefore retain a delegate after QS3D had forgotten the exact document/handler generation, leaving no deterministic retry path and allowing stale callbacks to reach sidecar or close-prompt logic.

## Correctness contract

The current generation is represented by an opaque attachment token keyed by the exact `Document` wrapper. Native ownership is stored separately by token and is published before crossing either fallible add accessor.

For each native event, `MayHaveSaveComplete` / `MayHaveBeginClose` is set before the corresponding `+=`. The bit is cleared only after the matching `-=` succeeds. A failed remove therefore retains the exact document/delegate pair for a later retry.

Detach revokes the active attachment token before attempting native removal. A retained old-generation callback first observes `DetachRequested`, retries its own detach and returns. A callback whose token is no longer current also requests detach and returns. Neither stale path may save a sidecar, show an unsaved-project prompt, veto a close, publish palette status, or reacquire authority from a replacement generation.

`DetachInProgress` prevents reentrant remove attempts from recursively operating on the same native subscription. Stop/document-destroy/idle/next-attach paths retry retained detach requests without clearing native ownership prematurely.

Existing sidecar semantics are otherwise preserved: successful `SaveComplete` may flush pending QS3D changes; close may save, discard or veto according to the existing prompt; save failures remain redacted and may attempt the existing recovery-copy path; active-document status publication remains fenced by `IsActiveDocument`.

## REMOTE_SAFE verification

Repository-safe verification for the exact candidate includes:

```text
python scripts/preflight-v25-project-persistence-retryable-detach.py
python scripts/preflight-v25-selection-sync-retryable-detach.py
python scripts/preflight-all.py --mode guards
```

The protected Shared CI candidate must also complete deterministic smoke, acquire and validate the admitted/trusted BricsCAD V25 reference generation, and compile `QS3D.BricsCAD.V25` against that admitted generation. A green hosted V25 compile proves source/reference compatibility only; it is not licensed-native runtime evidence.

## LOCAL_ONLY qualification

The irreducible runtime scenario requires a licensed BricsCAD V25 host capable of exercising native event accessor failure/teardown behavior. A valid local qualification should bind to one exact pushed source/package identity and cover at minimum:

1. partial `SaveComplete`/`BeginDocumentClose` registration failure where compensation removal is rejected;
2. detach/remove rejection during document teardown followed by a later successful retry;
3. detach then reattach/replacement generation on the same logical document lifecycle, proving a retained old callback cannot save, prompt, veto, refresh UI, or target the replacement project;
4. normal SaveComplete sidecar persistence and normal close Yes/No/Cancel behavior as controls;
5. multi-document activation/close ordering and final zero-owned-handler/process residue where observable.

Until those cells run in a real licensed host, classify them `LOCAL_ONLY / NO_RESULT`. Never infer `LOCAL_PASS` from source inspection, deterministic preflight, hosted Shared CI, or admitted-reference compilation.

## Reservation note

`docs/LOCAL-AGENT-INBOX.md` is not modified by this carrier because that shared queue path is currently owned by another active Reservation-v2 carrier. Do not evade that collision. When the queue path becomes available, the exact merged/pushed candidate and this matrix can be registered there without changing the runtime classification recorded here.
