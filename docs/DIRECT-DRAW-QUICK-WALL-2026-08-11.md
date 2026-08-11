# QS3D Direct Draw — Quick Wall

Updated: 2026-08-11 (UTC+7)

## Why this exists

Owner feedback is that the quantity/XLSX + Locate workflow is already useful, while model authoring still takes too many interactions for a basic wall. The first productivity fix therefore targets the highest-frequency straight-wall path without weakening semantic ownership, quantity, regeneration or native rollback behavior.

## Primary wall workflow

`QS3DDRAWWALL` is now the fast default used by the existing **Vẽ Tường** entry points.

```text
Vẽ Tường / QS3DDRAWWALL
-> pick point 1
-> pick point 2
-> use the active compatible ArchitecturalWall Family values
-> create the real LINE source
-> semantic capture
-> semantic regeneration
-> owned native wall build
```

For the normal two-point wall there is no extra "Enter to finish path" step and no mandatory thickness/height/bottom-offset prompt sequence after the second point.

The quick path reads, without mutating the project, these values from the active/preferred compatible Family when available:

- `ThicknessM`;
- `HeightM`;
- `BottomOffsetM`.

On a drawing with no existing project/Family, the current starter-compatible fallbacks remain `0.2 m`, `3.6 m`, and `0 m`. Explicit malformed/non-finite Family values still fail closed before source CAD is created.

The existing Ribbon/Hub mapping does not need a second primary button: it already launches `QS3DDRAWWALL`, so changing that command's default workflow makes the current **Vẽ Tường** action fast immediately after the updated plugin is loaded.

## Advanced / chain wall workflow

The previous flexible authoring flow is preserved as:

`QS3DDRAWWALLADV`

Use it when the user needs:

- two or more points / open-POLYLINE wall path;
- explicit per-operation thickness input;
- explicit height input;
- explicit source-relative bottom offset input.

This keeps fast day-to-day authoring and deliberate custom authoring separate instead of making every straight wall pay the interaction cost of the advanced case.

## Safety and model invariants

Quick Wall deliberately reuses the existing `ExecuteDirect` path. It does not introduce a second wall engine or semantic store.

The operation therefore still converges through:

- real BricsCAD LINE source provenance;
- `SemanticCaptureService`;
- `ProjectStateSnapshot` rollback;
- deterministic semantic regeneration before native mutation;
- existing `WallSolidBuilder` ownership/replacement behavior;
- ownership/XData-scoped CAD cleanup on failure;
- existing BQ/XLSX/Locate semantic references after creation.

Model Space, planar-UCS, finite-number and 5 mm planarity guards remain unchanged.

## Runtime qualification boundary

This change is source-side implementation. It is not a claim of licensed BricsCAD V25 runtime qualification.

The existing `LOCAL-008 — Direct Draw transient preview and repeated mode` gate remains the local UX qualification owner. For this source delta, local qualification should distinguish:

1. `QS3DDRAWWALL`: cancel on point 1 or point 2 leaves no project/source/semantic/native residue; successful two-point creation does not show mandatory numeric prompts and uses the expected active Family values;
2. `QS3DDRAWWALLADV`: the previous point-chain and numeric-prompt cancel matrix remains available and rollback-safe;
3. Ribbon/Full Domain Hub **Vẽ Tường** still invokes the fast `QS3DDRAWWALL` path;
4. save/reopen, Health, quantity/XLSX and Locate continue to see the new wall through the normal semantic model.

Transient preview, continuous/repeated drawing, richer dynamic input and exact native editor behavior remain LOCAL_ONLY work under the existing Direct Draw UX gate.

GitHub Actions remain manual-only under `CI_POLICY.md`; this source/docs change does not authorize a workflow run.