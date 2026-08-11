# QS3D Direct Draw — Quick Reference Wall

Updated: 2026-08-11 (UTC+7)

## Goal

Make the reference-driven wall workflow useful for rapid tracing without forcing four numeric confirmations for the normal case.

The selected reference LINE stays **read-only**. QS3D creates its own new wall source LINE from the reference direction/center, then uses the normal semantic/native wall pipeline.

## Quick command

Primary command: `QS3DDRAWWALLREF`

```text
Tường theo tham chiếu
-> select reference LINE
-> keep the reference LINE plan length
-> use active/preferred ArchitecturalWall Family / Type
-> create a new QS3D source LINE
-> semantic capture
-> scoped regeneration
-> WallSolidBuilder
-> owned native wall
```

The primary quick path uses:

- `LengthM` from the selected reference LINE;
- Family `ThicknessM`;
- Family `HeightM`;
- Family `BottomOffsetM`.

There is no mandatory Length / Thickness / Height / BottomOffset prompt sequence for the normal command.

## Advanced command

Use `QS3DDRAWWALLREFADV` when the new wall must differ from its reference or active Family.

The advanced flow preserves the previous explicit prompts for:

- wall length;
- wall thickness;
- wall height;
- source-reference bottom offset.

The requested wall remains centered on the reference direction as before.

## Safety

Quick Reference Wall changes only the interaction surface. It preserves the established lifecycle:

- Model Space guard;
- reference LINE opened read-only and never repurposed as QS3D ownership;
- finite/unit-aware reference planarity and length checks;
- read-only Family lookup before project creation;
- `ProjectStateSnapshot` rollback;
- real new DWG source LINE with stable Handle;
- `SemanticCaptureService`;
- canonical `ProjectElement.SetProperty()` writes;
- deterministic **operation-scoped** semantic regeneration of only the newly-created wall before native mutation;
- `WallSolidBuilder.BuildSelectedLineWalls`;
- live generated-handle verification;
- ownership/XData-verified generated cleanup before project restore;
- post-commit UI synchronization kept outside the rollback-critical operation.

The reference object is never erased by a failed authoring rollback because it is not operation-owned source CAD. An unrelated dirty semantic element is also not consumed as a side effect of drawing one reference wall; it remains dirty for its own workflow.

## Runtime qualification boundary

This is source/static-contract work. Exact V25 interactive proof remains under `LOCAL-008`.

Local qualification should cover:

1. `QS3DDRAWWALLREF`: cancel reference selection leaves no project/source/semantic/native residue; successful selection proceeds without numeric prompts and keeps the reference length + compatible Family values;
2. `QS3DDRAWWALLREFADV`: cancel independently at Length / Thickness / Height / BottomOffset prompts and verify no residue;
3. reference LINE remains unchanged after success and forced failure;
4. new source/generated ownership is distinct from the reference;
5. begin with an unrelated semantic element already dirty, create one reference wall, and verify only the newly-created wall is regenerated before native build while the unrelated element remains dirty;
6. save/reopen, BQ/XLSX/Locate, Health and rebuild continue through the normal semantic model;
7. active-DWG switching and forced native failure remain fail-closed.

The source scope is locked by `scripts/preflight-quick-reference-wall-authoring.py`; this does not replace exact BricsCAD V25 interaction evidence.

Transient preview, repeated authoring and native editor behavior remain LOCAL_ONLY.

GitHub Actions remain manual-only under `CI_POLICY.md`; this source/docs batch does not authorize workflow dispatch.
