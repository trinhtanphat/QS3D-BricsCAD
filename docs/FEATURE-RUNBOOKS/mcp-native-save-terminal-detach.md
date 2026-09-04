# Native QSAVE terminal detach truthfulness

Issue: #5628
Lane-Key: issue-5628
Runtime classification: REMOTE_SAFE source/static/V25 compile. Licensed BricsCAD unsubscribe-failure injection remains LOCAL_ONLY / NO_RESULT.

## Defect

A native `QSAVE` terminal event is not by itself sufficient proof that the save operation is lifecycle-clean. BricsCAD event unsubscription can fail. If a worker reports success solely from `CommandEnded` plus clean `DBMOD`, stale terminal handlers can remain subscribed after a reported-success save.

## Contract

1. Queue `QSAVE` exactly once through the existing native mutation coordinator; cleanup uncertainty must never replay the command.
2. Await the terminal callback outside BricsCAD application context.
3. After terminal publication, re-enter application context as a serialization barrier and require `DetachBestEffort()` to prove all owned terminal handlers are unsubscribed.
4. If detach cannot be proven, fail closed with redacted uncertainty/no-auto-retry guidance. Do not interpret terminal success or clean `DBMOD` as a successful tool result.
5. Only after cleanup is proven may the worker surface terminal cancellation/failure or verify active-document/path and persistent `DBMOD` bits.
6. The final cleanup path remains idempotent and disposes `Done` only when handler ownership is proven released.

## Deterministic regression

Run:

```bash
python scripts/preflight-mcp-native-save-terminal-detach.py
```

The guard verifies post-terminal detach proof occurs before terminal/result success checks, the callback attempts detach before publishing `Done`, no-auto-retry guidance remains present, and native QSAVE retains exactly one dispatch site.

## Validation boundary

Hosted CI/static/V25 compile can validate source topology and compilation. It cannot truthfully claim licensed BricsCAD behavior when native event removal itself is forced to fail. That injection remains LOCAL_ONLY / NO_RESULT until executed on a licensed compatible host.