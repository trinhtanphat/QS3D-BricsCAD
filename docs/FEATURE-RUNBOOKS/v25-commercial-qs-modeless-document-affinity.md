# BricsCAD V25 Commercial QS modeless document affinity

## Scope

This runbook covers `QS3DCOMMERCIAL` modeless ownership in `src/QS3D.BricsCAD.V25/CommercialQsCommands.cs`. The launcher owns a `CommercialQsWindow` across BricsCAD host callbacks and must not reuse or publish a window that belongs to a stale managed `Document` or native database generation.

## REMOTE_SAFE contract

Static/deterministic validation must prove all of the following:

- ownership is represented by a pending/published owner object, not a raw static `Window` plus a global database pointer;
- the owner retains the exact managed `Document` identity with a weak reference and the exact non-zero native database identity captured for that document generation;
- a published window is reused only when it is loaded and both managed-document identity and native-database identity match;
- pending ownership is installed before `Application.ShowModelessWindow`, so a reentrant invocation cannot create an unrooted duplicate;
- exact active-document/native-generation authority is revalidated after destructive close work and after `ShowModelessWindow` host pumping, before publication or status writes;
- publication requires the window to remain loaded and the exact pending owner to still own the publication slot;
- `Closed` captures an immutable owner token and releases only that owner;
- close exceptions and native close veto retain ownership while the window remains loaded; ownership is released only after terminal unload;
- errors/status do not include raw exception messages and are suppressed when the originating document generation is no longer current.

Run the focused source guard:

```text
python scripts/preflight-v25-commercial-qs-modeless-document-affinity.py
```

The repository aggregate feature-source guard must also remain green, including `scripts/preflight-commercial-qs-workspace.py`.

## Admitted-reference V25 compile

A hosted compile against the repository-admitted BricsCAD V25 references is REMOTE_SAFE evidence that the source remains compatible with the supported V25 API surface. It is not evidence that BricsCAD modeless runtime behavior passed.

## LOCAL_ONLY qualification

The following scenarios require a real licensed BricsCAD V25 runtime and must remain `LOCAL_ONLY / NO_RESULT` until actually exercised:

1. Open drawing A, launch `QS3DCOMMERCIAL`, switch to B, then return to A while native modeless callbacks are pumping. Confirm a stale A wrapper/generation is never reused after document replacement/reload.
2. Force an A→B→A timing change during `Close()` and during `Application.ShowModelessWindow`. Confirm no stale candidate is published and no success/failure status is written through the superseded document.
3. Exercise a native close veto/failure. Confirm the still-loaded window remains rooted and a second invocation cannot create a duplicate modeless window.
4. Close the window normally. Confirm pending/published ownership is released exactly once and a subsequent invocation creates one fresh window.
5. Exercise disposed/native-wrapper timing and verify that failures are redacted to exception type only and do not escape the command.

Never label these scenarios `LOCAL_PASS` from hosted CI, static guards, deterministic tests, or an admitted-reference compile.
