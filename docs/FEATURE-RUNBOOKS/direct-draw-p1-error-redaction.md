# BricsCAD V25 Direct Draw authoring failure redaction and post-commit truth

Lane: C03 — BricsCAD V25 UI / Modeless / Authoring  
Lane-Key: `issue-6047`

## Scope

This contract covers these V25 authoring surfaces:

- `DirectDrawP1Commands`
- `DirectDrawOpeningCommands`
- `DirectDrawWindowCommands`
- `DirectDrawSlabOpeningCommands`
- `DirectDrawUiFailureReporter`

The change is presentation-boundary hardening only. It must not alter semantic capture, CAD/native geometry creation, Auto Host, slab boolean behavior, transaction ownership, rollback scope, prompt cancellation, UCS/unit checks, project affinity, or generated-handle ownership.

## Failure classification

### Pre-commit / authoring failure

An exception that reaches a command `Guard` is reported with a stable operation-level diagnostic. Raw exception text, paths, handles, nested messages, or `Exception.Message` are not published to the BricsCAD Editor or process-wide Palette.

The reporting path is best-effort and cannot throw through the command boundary. Editor publication and Palette publication are isolated independently.

### Post-commit UI failure

`FinalizeUi` is reached only after that command's authoring success path has completed. A failure while refreshing the palette, restoring selection, regenerating the editor, or publishing status is presentation-only. It must be reported as:

> Direct Draw đã commit nhưng đồng bộ giao diện chưa hoàn tất. Hãy refresh giao diện.

Do not describe this state as an authoring rollback/failure and do not attempt to roll native or semantic work back from the reporting boundary.

## Document affinity

The BricsCAD Editor warning is sent to the command's captured source `Document` on a best-effort basis. Process-wide `PaletteCoordinator.SetStatus` is stricter: it is allowed only when the exact source `Document` is still `Application.DocumentManager.MdiActiveDocument` by reference identity.

This prevents a stale command from document A from smearing its presentation status onto active document B.

## Lifetime and thread constraints

`DirectDrawUiFailureReporter` is synchronous and stateless. It must not:

- subscribe to application/document/WPF events;
- queue Dispatcher/idle work;
- retain `Document`, `ProjectState`, selection, or entity wrappers;
- start CAD transactions or acquire document locks;
- create/replace project state;
- mutate implied selection or geometry.

## REMOTE_SAFE verification

Run the discovered source guard:

```text
python scripts/preflight-direct-draw-p1-error-redaction.py
```

Then run the repository required CI and the admitted-reference BricsCAD V25 compile path. A remote compile/guard GREEN proves source/static compatibility only.

## LOCAL_ONLY verification

On a real licensed BricsCAD V25 runtime, exercise representative P1, Door/WallOpening, Window, and slabOpen authoring flows and failure injection around:

1. pre-commit command failure;
2. rollback cleanup failure;
3. document switch before user-visible failure publication where the host permits it;
4. post-commit palette refresh failure;
5. post-commit implied-selection/regen/status failure.

Expected result: no raw exception detail is displayed; committed authoring remains committed after UI-only failure; stale source-document status never overwrites another active document's Palette.

Do not record `LOCAL_PASS` unless those checks were executed on a licensed BricsCAD V25 runtime.
