# QS3D BricsCAD V25 master plan

## Implemented source baseline

- clean-room layered architecture: `QS3D.Core` + BricsCAD V25 adapter + WPF/Ribbon UI;
- `.qsdb` semantic source-of-truth with schema v3 migration, locking, validated temp save, backup/recovery, persisted dirty state, project QuantityRules and audit provenance;
- dependency/fixed-point regeneration, formula engine, project rule catalog, Model Health and revision foundations;
- active Zone/Floor/Family semantic property flow and multi-DWG project cache keyed by `Document` identity;
- Room / Tường KT / Opening / Door / HT_Phòng capture and quantity workflows;
- Beam / Slab / Column / StructuralWall / Foundation / Stair / Railing / Earthwork deterministic quantity paths;
- native wall/structural Solid3d source paths isolated behind the V25 adapter and two-phase generated-geometry replacement;
- Quick Takeoff Length/Area/Volume/Count with `INSUNITS` conversion;
- BQ stable-ID grouping/filtering/Locate/XLSX, real recalc and persisted visible-column preferences;
- rebar notation/BBS model, XLSX/CSV export and BBS review/Locate UI;
- persisted revision baseline/diff workflow using `.qsrev`;
- deterministic recognition review + confident auto-apply, with project layer mappings overriding fallback heuristics;
- `.qstemplate` company-standard import/export for Families, QuantityRules, layer mappings, BQ columns and generic material/classification properties;
- deterministic planar room-boundary engine with iterative bridge detection/source lookup: intersection/T-junction subdivision, endpoint snapping, dangling-bridge removal, bounded-face traversal, stable boundary keys and Area/Perimeter calculation;
- `QS3DROOMAUTO` accepts selected LINE/POLYLINE networks including polyline bulges; bulges are tessellated in metric Core geometry with configurable maximum sagitta before face discovery;
- Room Auto lifecycle is non-destructive and quantity-safe: same normalized source provenance reuses the existing Room, topology split/merge marks superseded Rooms `Stale`, stale Rooms/direct dependents are excluded from BQ, and audit records remain available for review/recovery;
- Room Auto boundary provenance is resolved by the adapter without claiming duplicate semantic `SourceHandles`; HT_Phòng can target auto Rooms from their full boundary selection and existing finish semantics are synchronized on room updates;
- Room Auto is exposed through command line, Ribbon and Full Domain Hub and the entire semantic operation is protected by project snapshot rollback if update/regeneration fails;
- V25 release package + per-user DemandLoad install/uninstall source with hashes/signature policy and proprietary-runtime exclusion;
- manual-only GitHub Actions and V25 self-hosted NETLOAD/runtime/screenshot harness.

## Next validation gates

1. Static/source preflight on the newest head.
2. Core Release build + deterministic smoke suite for the newest Room Auto lifecycle head when explicitly run; earlier green runs do not automatically validate later commits.
3. Licensed Windows BricsCAD V25 compile on `[self-hosted, windows, x64, bricscad-v25]`.
4. `NETLOAD`/DemandLoad and command/Ribbon/palette regression, including `QS3DROOMAUTO`, recognition/template/revision/BBS/domain/audit workflows.
5. Private sample DWG regression: wall/room/auto-room/opening/finish/structural/takeoff/BQ/BBS/template/save/reopen, including curved boundaries and Room Auto split/merge/reuse cases.
6. Visual regression at 100/125/150/200% DPI with Vietnamese Unicode.
7. Performance/multi-DWG open-activate-SaveAs-close corpus plus large planar boundary-network corpus.
8. Only after these gates are green, consider automatic PR CI/release-candidate automation.

## Runtime-dependent product completion

- robust native 3D Beam/Slab/Column/StructuralWall/Foundation/Stair authoring beyond current guarded source paths;
- polyline wall corners, joins/T-junctions and freeform wall profiles;
- physical opening/door boolean subtraction from host solids;
- V25/private-DWG proof and performance tuning of automatic room-boundary discovery; optional direct ARC/SPLINE source support beyond LINE/POLYLINE inputs;
- geometric rebar placement/shape generation tied to BBS;
- transient highlight/isolate/section-box UX proven against V25 editions;
- Authenticode production signing and signed updater;
- optional Cloudflare license/update/team-sync backend;
- future AutoCAD adapter reusing `QS3D.Core`.

These remaining items must not be marked complete from source review alone when their correctness depends on BricsCAD V25 runtime, private DWG data, signing infrastructure or external deployment.
