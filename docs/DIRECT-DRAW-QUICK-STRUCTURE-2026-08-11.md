# QS3D Direct Draw — Quick Structure

Updated: 2026-08-11 (UTC+7)

## Goal

Reduce the number of interactions required for high-frequency Beam, Slab and Column authoring without weakening the existing semantic, quantity, generated-ownership, rollback or native-builder contracts.

The normal workflow should use the active compatible **Family / Type** values and let the user focus on geometry. Explicit per-operation numeric entry remains available through separate advanced commands.

## Quick Beam

Primary command: `QS3DDRAWBEAM`

```text
Vẽ Dầm
-> pick point 1
-> pick point 2
-> use active/preferred Beam Family values
-> source LINE
-> semantic capture
-> regeneration
-> native 3D
```

The primary command no longer requires Width / Height / BottomOffset prompts after the second point. It reads `WidthM`, `HeightM` and `BottomOffsetM` from the compatible Family when available, otherwise it uses the existing starter-compatible fallbacks.

Use `QS3DDRAWBEAMADV` when dimensions or source-relative offset must be entered for that individual operation.

## Quick Slab

Primary command: `QS3DDRAWSLAB`

The user still picks the closed slab boundary because the geometry itself requires those points, then presses Enter to finish the boundary. After the boundary is accepted, the primary quick command does not add another Thickness / BottomOffset prompt sequence.

It uses the active/preferred Slab Family `ThicknessM` and `BottomOffsetM`, creates the real closed POLYLINE source and reuses the existing semantic/native path.

Use `QS3DDRAWSLABADV` for explicit per-operation thickness and bottom offset.

## Quick Column

Primary command: `QS3DDRAWCOLUMN`

```text
Vẽ Cột
-> pick center
-> use active/preferred Column Family section + height + offset
-> source rectangular POLYLINE
-> semantic capture
-> regeneration
-> native 3D
```

The primary command uses Family `WidthM`, `DepthM`, `HeightM` and `BottomOffsetM`, so a normal rectangular column requires only its insertion point after the Family / Type has been chosen.

Use `QS3DDRAWCOLUMNADV` for explicit Width / Depth / Height / BottomOffset entry.

## Product behavior

The existing primary Ribbon and Full Domain Hub actions keep their current command names. Therefore `Vẽ Dầm`, `Vẽ Sàn` and `Vẽ Cột` automatically route to the fast paths after the updated plugin is loaded; there is no need to add more primary buttons and clutter the UI.

Advanced commands are intentionally secondary. The expected day-to-day pattern is:

```text
choose Family / Type once
-> draw many normal objects with primary quick commands
-> use *ADV only for exceptions
```

This is the same interaction principle as the current Quick Wall implementation.

## Safety and architecture

Quick Structure deliberately reuses `ExecuteDirect`; it does not add a parallel model or geometry engine.

The operation continues through:

- read-only Family/default lookup before project creation;
- Model Space and planar-UCS guards;
- real BricsCAD LINE/POLYLINE source provenance;
- `SemanticCaptureService`;
- `ProjectStateSnapshot` rollback;
- deterministic semantic regeneration before native mutation;
- `StructuralSolidBuilder` for Beam / Slab / Column;
- generated ownership/XData-scoped cleanup on failure;
- normal project/BQ/XLSX/Locate downstream semantics.

Malformed explicit Family numeric values still fail closed instead of being silently replaced by a fallback.

## Runtime qualification boundary

This is source implementation and static-contract coverage only. It is not a claim of licensed BricsCAD V25 runtime qualification.

`LOCAL-008 — Direct Draw transient preview and repeated mode` remains the runtime UX owner. For this source delta, local testing must distinguish:

1. `QS3DDRAWBEAM`: point-1 / point-2 cancellation, no mandatory numeric prompts on success, correct Family defaults;
2. `QS3DDRAWBEAMADV`: the previous Width / Height / BottomOffset prompt and cancellation matrix;
3. `QS3DDRAWSLAB`: boundary cancel/finish behavior, no mandatory numeric prompts after an accepted boundary, correct Family defaults;
4. `QS3DDRAWSLABADV`: the previous Thickness / BottomOffset prompt and cancellation matrix;
5. `QS3DDRAWCOLUMN`: center-point cancellation, no mandatory numeric prompts on success, correct Family section/height/offset;
6. `QS3DDRAWCOLUMNADV`: the previous Width / Depth / Height / BottomOffset prompt and cancellation matrix;
7. Ribbon/Domain Hub primary buttons still invoke the quick command names;
8. save/reopen, Health, quantities/XLSX, Locate and supported downstream rebar workflows continue to resolve the created objects normally.

True transient DrawJig/live profile preview, continuous/repeated authoring, native editor behavior and document-switch interaction remain LOCAL_ONLY and must not be marked complete from remote source inspection.

GitHub Actions remain manual-only under `CI_POLICY.md`; this source/docs batch does not authorize workflow dispatch.
