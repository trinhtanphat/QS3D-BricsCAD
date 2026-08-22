# BLT-style completion review — 2026-08-10

This review records the source-level BLT-completion work applied directly to `main`. It preserves concurrent agent work and does not claim licensed BricsCAD V25 runtime verification.

## Review scope

- Core/domain contracts used by UI, semantic selection and geometry adapters.
- Main WPF Workspace, Family/Instance inspector, selected-object review, Ribbon and Full Domain Hub.
- Room Auto, Tường KT, wall junction/snap cleanup, Door/Opening host matching/cutting and current rebar geometry paths.
- Static source/preflight/documentation consistency.
- Concurrent `main` activity was re-read before writes; stale writes were rejected/rebased instead of force-overwriting newer work.

## Completed / hardened in the continued review

### Family / Instance BLT property workflow

- Property rows now expose typed metadata for text, boolean and editable-choice editors instead of treating every property as a plain TextBox.
- The inspector explicitly separates **Family / Type** and **Đối tượng / Instance** scope.
- Exactly one semantic selection switches into Instance scope; ambiguous multi-element matches stay out of instance editing.
- Instance overrides can be reset to the current Family value from the row.
- Family edits update still-inherited values but preserve true instance overrides instead of overwriting every member element.
- Opening/rebinding the Family inspector no longer dirties the project merely because the bound Family name is loaded.
- Selection synchronization uses `SemanticReferenceHandles`, including Auto Room boundary provenance/generated-solid fallback.

### Workspace / Ribbon / Hub parity

- Workspace selected-object review exposes Focus, Cô lập, Khôi phục and Locate/Top-view actions.
- Workspace exposes **Giao tường**, **Snap xem**, **Snap áp** and **Auto Host** directly beside the main Family/modeling actions.
- Ribbon exposes wall junction/snap, Auto Host, Highlight/Focus/Isolate and both column/shape rebar workflows.
- Full Domain Hub exposes the same major advanced workflows, reducing command-line-only features.

### Tường KT and wall cleanup

- Tường Gạch, Vách Kính and Trụ Tường share guarded LINE/open-POLYLINE native 3D paths.
- `QS3DWALLJUNCTIONS` classifies L/T/X/Straight/End/Multi centerline nodes.
- Concurrent wall cleanup work adds `QS3DWALLSNAPPREVIEW` / `QS3DWALLSNAPAPPLY`: endpoint mutation is review-gated and fingerprinted; Apply rejects stale previews and unsupported curved/bulged/nonsemantic sources.
- Generated semantic geometry invalidated by source-wall mutation is handled with ownership-aware invalidation rather than silently left stale.
- This closes part of the earlier wall-junction gap, but does **not** yet claim complete automatic physical solid union/reconciliation at every L/T/X/Multi junction.

### Door / Opening

- Physical-cut fingerprints include live host/opening geometry and parameters, preventing a moved opening from being mistaken for an already-applied cut.
- Physical cutting covers compatible LINE hosts and guarded straight/non-bulged POLYLINE segments that safely project to one segment; curved/bulged/corner-crossing cases fail closed.
- `QS3DAUTOLINKHOSTS` automates only the semantic host-link step using surface gap, Floor/Zone compatibility, ambiguity rejection and an independent elevation gate. It never silently runs the physical boolean cut.
- Workspace/Ribbon/Hub expose both Auto Host and explicit manual/cut flows.

### Room Auto

- Preserved direct planar ARC and bounded SPLINE source adapters alongside LINE/POLYLINE.
- Curve sampling has configurable sagitta/chord limits and hard caps; source elevations/planarity are validated.
- Auto Room lifecycle remains non-destructive with source provenance, stale-room handling, rollback and semantic-reference Locate behavior.

### Rebar

- Preserved rectangular-column 3D bar generation, deterministic linear count/spacing planning and ownership/health checks.
- BBS-shape-driven geometry supports guarded straight/L/U/Z/custom leg/turn source paths through `QS3DREBAR3DSHAPE` with separate shape ownership/health metadata.
- Ribbon/Hub expose column and shape rebar geometry/health flows.

### Regression / documentation guards

- Geometry/full-domain/advanced/room/wall-specific preflights continue to guard geometry and ownership contracts.
- Added `scripts/preflight-blt-workspace.py` to check Family/Instance scope, typed editors, semantic selection sync, Focus/Isolate, Giao tường, wall snap and Auto Host entry-point parity plus key XAML well-formedness.
- README, COMMANDS, IMPLEMENTATION-STATUS, PLAN, UI-SPEC and ADVANCED-GEOMETRY are being kept aligned with the source-level/runtimed-gated distinction.

## Preserved concurrent fixes

The review intentionally preserved concurrent changes including:

- Auto Host with ambiguity/elevation safety;
- wall snap preview/apply plus atomic generated-geometry invalidation;
- straight-POLYLINE opening cuts;
- far-origin-safe wall/opening/junction math;
- shape rebar source/ownership/health hardening;
- Room Auto ARC/SPLINE lifecycle hardening;
- shared semantic reference-handle Locate behavior.

No force update was used to overwrite these changes.

## Remaining high-value BLT parity work

1. Licensed V25 compile + NETLOAD/DemandLoad and private-DWG regression on the newest head.
2. Real V25 screenshots at 100/125/150/200% DPI and follow-up spacing/icon/context-menu/focus polish.
3. Production-grade Vách Kính curtain-wall framing/panels and specialized Trụ Tường profiles/material presentation.
4. Complete physical wall-solid reconciliation/union at L/T/X/Multi junctions beyond the current guarded source-centerline snap workflow.
5. Curved/bulged polyline-host opening booleans and complex corner-spanning openings.
6. Broader beam/slab/wall/stirrup/hook/bend-radius rebar authoring and editing.
7. Specialized material/level/classification editors, section-box workflow, commercial icons/shortcuts/context menus and proven large-model virtualization.
8. Authenticode/signed updater and optional commercial backend.

GitHub Actions remain manual-only. This review updates source/static guards and documentation but does not substitute for licensed BricsCAD V25 runtime proof.
