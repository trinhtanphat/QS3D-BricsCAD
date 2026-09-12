# BricsCAD V25 Estimating modeless document-generation affinity

## Scope

This runbook covers `QS3DESTIMATE` / `QS3DRATEBUILDUP` modeless ownership in `src/QS3D.BricsCAD.V25/EstimatingRateBuildUpCommands.cs`. The launcher must not reuse, publish, or report through a modeless estimating window that belongs to a stale managed `Document` or native BricsCAD database generation.

## REMOTE_SAFE contract

Static/deterministic validation must prove all of the following:

- window authority is represented by an owner that retains the exact managed `Document` through a weak reference plus its exact non-zero `Database.UnmanagedObject` identity;
- unpublished ownership is installed before `Application.ShowModelessWindow`, so reentrant command entry cannot publish or leak a duplicate candidate;
- a published window is reused only when it remains loaded and its owner matches both the exact managed document and native database generation;
- exact active-document/native-generation authority is revalidated before modeless hosting and again after `ShowModelessWindow` host pumping, before publication;
- a stale or unloaded post-host candidate is terminal-close attempted and is never published as the singleton;
- native close exceptions or close veto retain ownership while the window remains loaded; ownership is released only after terminal unload/`Closed`;
- publication-in-flight and cleanup-in-flight state block reentrant duplicate creation;
- editor output and palette status are generation-safe: authority is checked before editor output and checked again before palette status publication because editor output may re-enter/pump host work;
- status/error publication is suppressed for a superseded document generation and raw exception messages are not exposed.

Run the focused source guard:

```text
python scripts/preflight-v25-estimating-modeless-document-affinity.py
```

The aggregate feature-source guards, package-integrity checks, deterministic smoke tests, and admitted-reference V25 compile must also remain green on the exact candidate SHA.

## Admitted-reference V25 compile

A hosted compile against repository-admitted BricsCAD V25 references is `REMOTE_SAFE` evidence that the implementation remains compatible with the supported V25 API surface. It does not prove native modeless behavior inside licensed BricsCAD.

## LOCAL_ONLY qualification

These scenarios require a real licensed BricsCAD V25 runtime and remain `LOCAL_ONLY / NO_RESULT` until actually exercised:

1. Open drawing A, launch `QS3DESTIMATE`, switch to B, then return to A while modeless hosting/status callbacks can pump host work. Verify a stale A wrapper or replaced native generation is never reused or published.
2. Reload/replace the native database for the same managed-document workflow around `Application.ShowModelessWindow`. Verify the post-host generation fence rejects the stale candidate and terminal-close is attempted.
3. Exercise native close veto/failure for both published and unpublished candidates. Verify a still-loaded window remains rooted and a subsequent invocation cannot create a duplicate.
4. Exercise reentrant invocation while publication and cleanup are in flight. Verify exactly one authoritative candidate/window exists and ownership releases exactly once after terminal `Closed`.
5. Force a document-generation change after `Editor.WriteMessage` but before palette status publication. Verify stale palette status is suppressed.
6. Exercise disposed/native-wrapper timing and verify the command fails closed without leaking raw exception messages.

Never label these scenarios `LOCAL_PASS` from hosted CI, static guards, deterministic tests, or an admitted-reference compile.
