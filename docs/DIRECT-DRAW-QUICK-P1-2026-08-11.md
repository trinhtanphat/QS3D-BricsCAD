# QS3D Direct Draw — Quick P1

Updated: 2026-08-11 (UTC+7)

## Goal

Continue the low-interaction Direct Draw direction for the guarded P1 categories that already have stable semantic/native builders: GlassWall, WallPier, StructuralWall and Foundation.

The primary commands now treat the active compatible **Family / Type** as the normal authoring configuration. Geometry remains interactive; repeated numeric prompts are moved to explicit `*ADV` commands for exceptions.

## Quick GlassWall

Primary: `QS3DDRAWGLASSWALL`

- pick two or more points;
- use active/preferred GlassWall `ThicknessM`, `HeightM`, `BottomOffsetM`;
- create LINE/open POLYLINE source;
- semantic capture;
- canonical `QS3DBUILD3D` backing host build.

No mandatory Thickness / Height / BottomOffset prompt sequence follows the accepted path. Use `QS3DDRAWGLASSWALLADV` when values must be entered for that operation.

Curtain frame/panel authoring remains the separate guarded Curtain workflow; this change does not duplicate it.

## Quick WallPier

Primary: `QS3DDRAWWALLPIER`

- pick exactly two points;
- use active/preferred WallPier thickness/height/bottom offset;
- create the same LINE source;
- preserve the specialized `WallPierProfileSolidBuilder` path through canonical `QS3DBUILD3D`.

Use `QS3DDRAWWALLPIERADV` for explicit values. Both quick and advanced commands remain two-point LINE-only; this optimization does not widen WallPier geometry support.

## Quick StructuralWall

Primary: `QS3DDRAWSTRUCTWALL`

```text
Vẽ Vách BTCT
-> point 1
-> point 2
-> Family / Type values
-> semantic + native 3D
```

The primary path no longer requires Thickness / Height / BottomOffset prompts. Use `QS3DDRAWSTRUCTWALLADV` for one-off values.

## Quick Foundation

Primary: `QS3DDRAWFOUNDATION`

The user still defines the closed plan boundary because that geometry is required. After the boundary is accepted, the primary path uses Family `ThicknessM` and `BottomOffsetM` directly instead of presenting more numeric prompts.

Use `QS3DDRAWFOUNDATIONADV` for explicit thickness/offset input.

## UI behavior

The current Ribbon and Full Domain Hub already route their main authoring buttons through the primary command names. Therefore the existing `Vẽ Vách Kính`, `Vẽ Trụ Tường`, `Vẽ Vách BTCT` and `Vẽ Móng` actions become the quick paths without adding another row of buttons.

The intended product pattern is consistent across P0/P1:

```text
choose Family / Type once
-> primary Direct Draw commands for normal objects
-> *ADV only for exceptions
```

This reduces repetitive modal work while keeping customization available.

## Safety and architecture

The quick P1 paths preserve the existing P1 `Execute(...)` orchestration rather than introducing a second native geometry system.

They retain:

- read-only Family/default lookup before project creation;
- Model Space and planar-UCS guards;
- real BricsCAD source entities and stable Handles;
- `SemanticCaptureService`;
- `ProjectStateSnapshot` rollback;
- canonical `ProjectElement.SetProperty()` dirty/stale behavior;
- active-DWG revalidation around nested `QS3DBUILD3D`;
- required live `GeneratedSolidHandle` verification;
- ownership/XData-scoped source/generated cleanup on failure;
- non-destructive post-commit UI synchronization.

Malformed Family values still fail closed. The change removes normal prompts; it does not weaken validation.

## Runtime qualification boundary

This source change must not be described as licensed BricsCAD V25 runtime-qualified until `LOCAL-008` is executed against the exact candidate SHA.

Local qualification for this delta should cover:

1. primary quick commands: cancel during geometry acquisition, no numeric prompts after accepted geometry, correct compatible Family values;
2. `QS3DDRAWGLASSWALLADV`, `QS3DDRAWWALLPIERADV`, `QS3DDRAWSTRUCTWALLADV`, `QS3DDRAWFOUNDATIONADV`: explicit prompt/cancel matrix remains rollback-safe;
3. WallPier quick and advanced remain two-point LINE-only and reach the specialized profile builder;
4. Ribbon/Domain Hub main buttons still invoke primary quick names;
5. save/reopen, regeneration, Model Health, BQ/XLSX/Locate and supported downstream workflows continue through the normal semantic model;
6. document switching and forced nested-build failure still cannot produce cross-DWG or partial state.

True transient preview, continuous/repeated drawing, DrawJig/editor lifecycle and exact interactive behavior remain LOCAL_ONLY.

GitHub Actions remain manual-only under `CI_POLICY.md`; this source/docs batch does not authorize workflow dispatch.
