# QS3D BricsCAD V25 master plan

## Implemented source baseline

- clean-room layered architecture: `QS3D.Core` + BricsCAD V25 adapter + WPF/Ribbon UI;
- `.qsdb` semantic source-of-truth with schema v3 migration, locking, validated temp save, backup/recovery, persisted dirty state, project QuantityRules and audit provenance;
- dependency/fixed-point regeneration, formula engine, project rule catalog, Model Health and revision foundations;
- active Zone/Floor/Family semantic property flow and multi-DWG project cache keyed by `Document` identity;
- Room / Tường KT / Opening / Door / HT_Phòng capture and quantity workflows;
- Beam / Slab / Column / StructuralWall / Foundation / Stair / Railing / Earthwork deterministic quantity paths;
- native wall/structural Solid3d source paths already isolated behind the V25 adapter and two-phase generated-geometry replacement;
- Quick Takeoff Length/Area/Volume/Count with `INSUNITS` conversion;
- BQ stable-ID grouping/filtering/Locate/XLSX, real recalc and persisted visible-column preferences;
- rebar notation/BBS model, XLSX export and BBS review/Locate UI;
- persisted revision baseline/diff workflow using `.qsrev`;
- deterministic recognition review + confident auto-apply, with project layer mappings overriding fallback heuristics;
- `.qstemplate` company-standard import/export for Families, QuantityRules, layer mappings, BQ columns and generic material/classification properties;
- manual-only GitHub Actions and V25 self-hosted NETLOAD/runtime/screenshot harness.

## Next validation gates

1. Static/source preflight on the newest head.
2. Core Release build + deterministic smoke suite when an execution environment is available; GitHub Actions remain manual and require explicit owner approval.
3. Licensed Windows BricsCAD V25 compile on `[self-hosted, windows, x64, bricscad-v25]`.
4. `NETLOAD` and command/Ribbon/palette regression, including recognition/template/revision/BBS workflows.
5. Private sample DWG regression: wall/room/opening/finish/structural/takeoff/BQ/BBS/template/save/reopen.
6. Visual regression at 100/125/150/200% DPI with Vietnamese Unicode.
7. Performance/multi-DWG open-activate-SaveAs-close corpus.
8. Only after these gates are green, consider automatic PR CI/release-candidate automation.

## Runtime-dependent product completion

- robust native 3D Beam/Slab/Column/StructuralWall/Foundation/Stair authoring beyond the current guarded source paths;
- polyline wall corners, joins/T-junctions and freeform profiles;
- physical opening/door boolean subtraction from host solids;
- automatic room-boundary discovery from arbitrary intersecting wall networks;
- geometric rebar placement/shape generation tied to BBS;
- transient highlight/isolate/section-box UX proven against V25 editions;
- installer/autoload package, code signing and signed updater;
- optional Cloudflare license/update/team-sync backend;
- future AutoCAD adapter reusing `QS3D.Core`.

These remaining items must not be marked complete from source review alone when their correctness depends on BricsCAD V25 runtime, private DWG data, signing infrastructure or external deployment.
