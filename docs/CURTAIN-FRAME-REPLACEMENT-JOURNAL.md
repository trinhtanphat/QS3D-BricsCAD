# Curtain frame CAD + semantic replacement journal

Updated: 2026-08-10 (UTC+7).

## Problem closed by this batch

The LINE and open/bulged-POLYLINE Curtain frame builders previously committed their BricsCAD transaction first and only then wrote `GeneratedCurtainFrameHandles`, counts, fingerprint/mode metadata, stale state and audit records into the in-memory `ProjectState`.

That ordering created a cross-layer failure window: a semantic ownership/audit exception after `transaction.Commit()` could leave newly committed frame `Solid3d` objects in the DWG while project metadata still described the previous frame set.

## Replacement journal contract

Both native frame builders now use the same ordering:

1. snapshot full `ProjectState` before native replacement starts;
2. validate the selected semantic/source batch and build all pending frame plans;
3. erase previous owned frame solids and append the new frame solids inside one BricsCAD transaction;
4. while that native transaction is still rollback-capable, apply the pending semantic ownership metadata, frame counts, configuration fingerprint, mode, stale-state clear, audit records and project touch;
5. commit the BricsCAD transaction;
6. if any semantic operation or native commit throws, restore the `ProjectState` snapshot while the failed BricsCAD transaction is aborted/disposed;
7. return only after both CAD and semantic ownership represent the same replacement generation.

This closes the frame-builder-specific `CAD committed -> semantic metadata failed` window without inventing a second generated-ownership schema.

## UI boundary

`CurtainWallFrameSolidBuilder` and `CurtainWallPathFrameSolidBuilder` no longer call `Editor.Regen()` after replacement. Viewport/Palette/status synchronization belongs to the command surfaces and remains best-effort/non-fatal after the successful replacement boundary.

A UI failure must not turn a valid CAD + semantic replacement into a false native failure.

## What this does not claim

This is a **frame-builder replacement journal**, not whole-command Curtain atomicity.

`QS3DCURTAIN3D` still composes separate native transaction families for:

- backing GlassWall host generation;
- LINE Curtain frame overlays;
- open/bulged-POLYLINE path frame overlays;
- live fingerprint stamping.

A later production step may add a higher-level orchestration journal if the product requires all of those families to roll back as one logical command. Until then, each builder must remain fail-closed on ownership/source errors and `QS3DHEALTHALL` / `QS3DRELEASECHECK` remain the consistency gate.

## Static guard

`scripts/preflight-curtain-frame-replacement-journal.py` is auto-discovered by `scripts/preflight-all.py` and requires both frame builders to:

- snapshot project state;
- apply pending semantic ownership before `transaction.Commit()`;
- restore the snapshot on failed operation;
- keep audit/project-touch inside that semantic journal;
- avoid post-commit `Editor.Regen()` inside the builder.

## Validation boundary

This change is source-reviewed/static-gated only in this conversation. GitHub Actions remain manual-only and were not dispatched. The exact final SHA still needs licensed BricsCAD V25 x64 compile/runtime/private-DWG qualification before production release claims.
