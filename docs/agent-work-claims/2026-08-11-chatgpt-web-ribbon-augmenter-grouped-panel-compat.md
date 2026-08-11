# Work claim — grouped Ribbon augmenter compatibility

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-ribbon-augmenters`
- Registered: `2026-08-11T20:29:00+07:00`
- Baseline main SHA: `010ca2006ada55bb3a122cc894979161e106ee4e`
- Priority: remove stale flat-panel fallbacks left in legacy Ribbon augmenters after the completed grouped Ribbon information architecture

## Reserved scope

Repair only the two remaining legacy augmenters whose source still targets removed flat panel IDs and silently falls back to the first panel after `RibbonBootstrapper` regrouped tabs into named functional panels.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/Ribbon/ReferenceWallRibbonAugmenter.cs`
- `src/QS3D.BricsCAD.V25/Ribbon/ProjectRibbonAugmenter.cs`
- `scripts/preflight-ribbon-augmenter-panel-targets.py` (new focused auto-discovered source gate)
- this claim file for close-out

`QuickWorkflowRibbonAugmenter.cs` is read-only evidence in this lane because its same defect was already repaired under the still-reserved Create Similar claim. `RibbonBootstrapper.cs` is read-only and remains owned by the completed Ribbon information-architecture design.

## Intended repair

- Reference Wall is an architectural authoring action; target exactly the existing grouped `QS3D_AUTHOR_ARCHITECTURE_PANEL_SOURCE` and fail closed if that panel is absent. Do not fall back to Setup/Structure/Output by enumeration order.
- ProjectRibbonAugmenter carries a broad Project Tools set that does not fit STATE/TEMPLATE/WORKSPACE cleanly; create/reuse one exact dedicated `QS3D_PROJECT_TOOLS_PANEL_SOURCE` / `Công cụ dự án` panel under the existing `QS3D_PROJECT` tab, idempotently, without editing `RibbonBootstrapper.cs`.
- Preserve click-time `MdiActiveDocument` routing and all existing command IDs/strings.

## Excluded scope

- No changes to `RibbonBootstrapper.cs`, Start Center Ribbon entry, QuickWorkflow/Create Similar command surfaces, Direct Draw, Core model/mutation/reporting, Workspace, local inbox, release or CI.
- No command rename/removal, no new business behavior, no Ribbon visual/runtime PASS claim.

## Validation plan

- Re-fetch both augmenter files immediately before writes.
- Add one focused static gate requiring exact grouped panel IDs, rejecting removed `QS3D_AUTHOR_PANEL_SOURCE` / `QS3D_PROJECT_PANEL_SOURCE` and the legacy `if (source == null) source = candidate;` fallback pattern.
- Guard Project Tools panel creation/reuse idempotence and active-document dispatch.
- Re-fetch current `main`/targets before each write; no force push or Actions dispatch.

## Coordination

The Ribbon information-architecture claim is `COMPLETED`; the Start Center Ribbon follow-up is already landing in `RibbonBootstrapper.cs` and is explicitly excluded here. The Create Similar claim remains `BLOCKED` on its canonical LOCAL-008 handoff and reserves only QuickWorkflow/Create Similar surfaces, not these two augmenters. Current Core reporting/mutation claims exclude BricsCAD Ribbon source.

## Completion condition

Both remaining augmenters stop relying on removed flat-panel IDs/fallback enumeration, focused static coverage is merged, final source remains in current `main` ancestry, and this claim is marked `COMPLETED` without claiming licensed BricsCAD V25 runtime proof or CI execution.
