# QS3D Direct Draw — Quick Reference Wall

Updated: 2026-08-11 (UTC+7)

## Goal

Make the reference-driven wall workflow useful for rapid tracing without forcing four numeric confirmations for the normal case or forcing the user to select the same reference LINE twice.

The selected reference LINE stays **read-only**. QS3D creates its own new wall source LINE from the reference direction/center, then uses the normal semantic/native wall pipeline.

## Quick command

Primary command: `QS3DDRAWWALLREF`

```text
Tường theo tham chiếu
-> if exactly one LINE is already preselected, consume that PICKFIRST selection
-> otherwise select one reference LINE interactively
-> keep the reference LINE plan length
-> use active/preferred ArchitecturalWall Family / Type
-> create a new QS3D source LINE
-> semantic capture
-> scoped regeneration
-> WallSolidBuilder
-> owned native wall
```

Both `QS3DDRAWWALLREF` and `QS3DDRAWWALLREFADV` opt into `CommandFlags.UsePickSet`. The resolver only consumes PICKFIRST when the implied selection contains **exactly one valid LINE**. Empty, multi-object, stale or non-LINE implied selection is not guessed from geometry and falls back to the existing explicit `GetEntity` prompt.

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

The requested wall remains centered on the reference direction as before. PICKFIRST only removes the redundant reference re-selection; it does not skip Advanced parameter prompts.

## Safety

Quick Reference Wall changes only the interaction surface. It preserves the established lifecycle:

- Model Space guard;
- PICKFIRST is read-only and accepted only for exactly one valid LINE; otherwise explicit selection remains the fallback;
- reference LINE opened read-only and never repurposed as QS3D ownership;
- finite/unit-aware reference planarity and length checks;
- reference acquisition still occurs before project preview/mutation, so cancel leaves no authoring project/source/semantic/native residue;
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

1. preselect exactly one valid LINE, launch `QS3DDRAWWALLREF`, and verify no second reference-selection prompt appears;
2. preselect zero, multiple, stale or non-LINE objects and verify the command safely falls back to the explicit LINE picker without mutating project/source/semantic/native state first;
3. `QS3DDRAWWALLREF`: cancel reference selection leaves no project/source/semantic/native residue; successful selection proceeds without numeric prompts and keeps the reference length + compatible Family values;
4. `QS3DDRAWWALLREFADV`: preselected LINE skips only reference re-selection, then cancel independently at Length / Thickness / Height / BottomOffset prompts and verify no residue;
5. reference LINE remains unchanged after success and forced failure;
6. new source/generated ownership is distinct from the reference;
7. begin with an unrelated semantic element already dirty, create one reference wall, and verify only the newly-created wall is regenerated before native build while the unrelated element remains dirty;
8. save/reopen, BQ/XLSX/Locate, Health and rebuild continue through the normal semantic model;
9. active-DWG switching and forced native failure remain fail-closed.

The source scope is locked by `scripts/preflight-quick-reference-wall-authoring.py` plus `scripts/preflight-reference-wall-pickfirst.py`; these do not replace exact BricsCAD V25 interaction evidence.

Transient preview, repeated authoring and native editor behavior remain LOCAL_ONLY.

GitHub Actions remain manual-only under `CI_POLICY.md`; this source/docs batch does not authorize workflow dispatch.
