# Work claim — grouped Ribbon augmenter compatibility

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-ribbon-augmenters`
- Registered: `2026-08-11T20:29:00+07:00`
- Completed: `2026-08-11T20:32:00+07:00`
- Baseline main SHA: `010ca2006ada55bb3a122cc894979161e106ee4e`
- Priority: remove stale flat-panel fallbacks left in legacy Ribbon augmenters after the completed grouped Ribbon information architecture

## Reserved scope

Repair only the two remaining legacy augmenters whose source still targeted removed flat panel IDs and silently fell back to the first panel after `RibbonBootstrapper` regrouped tabs into named functional panels.

## Source implementation

- `e2ed78ed951d35315c4068f7a27114c6d22db2c0` — `fix(ribbon): target grouped architecture panel`
  - `ReferenceWallRibbonAugmenter` now targets exactly `QS3D_AUTHOR_ARCHITECTURE_PANEL_SOURCE` for `QS3DDRAWWALLREF`;
  - if the grouped Architecture panel is absent it fails closed instead of appending to Setup/Structure/Output by enumeration order;
  - existing ID/command duplicate checks and click-time active-DWG routing are preserved.
- `716f065cc7f57f60b2e67ff29016fc56ed4233a9` — `fix(ribbon): isolate project tools panel`
  - `ProjectRibbonAugmenter` now creates/reuses exact `QS3D_PROJECT_TOOLS_PANEL_SOURCE` / `Công cụ dự án` under the existing `QS3D_PROJECT` tab;
  - the panel is idempotent by source ID and button IDs, so project tools no longer spill into the first STATE/TEMPLATE/WORKSPACE panel.
- `6afe29b4e6bf8acac44210f607496ae07d819636` — `test(ribbon): guard grouped augmenter panel targets`
  - new auto-discovered source gate verifies current grouped `RibbonBootstrapper` IDs, Reference Wall's exact Architecture target, Project Tools panel creation/reuse, the already-fixed Quick Workflow dedicated panel, click-time `MdiActiveDocument` dispatch and initialization order;
  - explicitly rejects removed `QS3D_AUTHOR_PANEL_SOURCE`, `QS3D_PROJECT_PANEL_SOURCE` and the legacy `if (source == null) source = candidate;` fallback pattern across all three legacy augmenters.

## Integration result

The completed Ribbon information architecture remains untouched: `RibbonBootstrapper.cs` was read-only in this lane. The later Start Center Ribbon follow-up also remains untouched. `PluginEntry` already initializes `RibbonBootstrapper` before Reference Wall, Project and Quick augmenters, so exact panel lookup/creation occurs only after the grouped tabs exist.

`QuickWorkflowRibbonAugmenter.cs` was read-only evidence here because its same flat-panel defect was repaired separately under the still-reserved Create Similar claim with a dedicated `QS3D_AUTHOR_QUICK_PANEL_SOURCE` / `Tác vụ nhanh` panel.

## Validation / runtime boundary

- Final Reference Wall and Project augmenter sources were re-fetched from `main` after their writes and contain the intended grouped/dedicated panel contracts.
- The focused Python source gate was authored and merged but not executed in this connector-only lane; no local checkout/build was available here.
- No GitHub Actions, release, installer, signing or licensed BricsCAD V25 runtime was dispatched/executed.
- Exact Ribbon rendering, idempotent reload, DPI/overflow and representative button dispatch remain LOCAL_ONLY under the existing Ribbon/V25 qualification process; no `LOCAL_PASS` is claimed.

## Coordination

The Ribbon information-architecture claim is `COMPLETED`; the Start Center Ribbon follow-up is outside these files. The Create Similar claim remains `BLOCKED` only on its canonical LOCAL-008 handoff and continues to reserve QuickWorkflow/Create Similar surfaces. Current Core reporting/mutation claims exclude BricsCAD Ribbon source.

## Completion condition

Satisfied for remote/source scope: both remaining augmenters use deterministic grouped/dedicated panel sources, focused regression coverage is merged, and no concurrent RibbonBootstrapper/Start Center work was overwritten. Native V25 evidence remains explicitly unclaimed/local-only.
