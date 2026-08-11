# QS3D Direct Draw — Active Family Quick / Advanced Draw

Updated: 2026-08-11 (UTC+7)

## Goal

Reduce command-switching overhead after the modeler has already chosen the working **Family / Type**.

QS3D now exposes two stable entry points:

```text
QS3DDRAWACTIVE      = normal Quick path
QS3DDRAWACTIVEADV   = one-off Advanced/custom path
```

The Quick Workflow Ribbon keeps **Vẽ Nhanh** as the primary action. Advanced stays secondary so the Ribbon is not filled with duplicate buttons.

The expected high-frequency loop becomes:

```text
choose Family / Type once
-> Vẽ Nhanh
-> acquire only the geometry required by that category
-> repeat the same Vẽ Nhanh entry point for normal objects
-> use Advanced only for an exception
```

This is complementary to the existing per-category commands. It does not remove them and does not replace any category-specific `*ADV` workflow.

## Workspace gestures

The Family / Type list exposes both stable entry points without forcing the modeler to move back to the Ribbon:

- **double-click a Family / Type** → make that row authoritative as the active Family and launch `QS3DDRAWACTIVE`;
- **Ctrl+D** while the Workspace has keyboard focus → launch Quick for the selected Family;
- **Ctrl+Shift+D** → launch the matching Advanced/custom path for the selected Family;
- Family context menu → **Vẽ Nhanh (Ctrl+D)** or **Vẽ tùy chỉnh (Ctrl+Shift+D)**.

These gestures reuse the same `WorkspaceViewModel.SetActiveFamily(...)` write already used by other Workspace authoring actions, then send one of the two active-family dispatcher commands. They do not duplicate per-category dispatch rules inside the Workspace.

The interaction intent is:

```text
Family list
-> select/double-click type
-> draw normally
-> Ctrl+Shift+D only when this instance needs custom parameters
```

instead of repeatedly moving between the Family palette and different Ribbon/command names.

## Dispatch contract

Both active-family commands read the existing active Family through `ProjectFamilyActivationService` and delegate to the already-guarded Direct Draw command for that category:

| Active Family category | `QS3DDRAWACTIVE` | `QS3DDRAWACTIVEADV` |
|---|---|---|
| ArchitecturalWall | Quick Wall | `QS3DDRAWWALLADV` behavior |
| Beam | Quick Beam | `QS3DDRAWBEAMADV` behavior |
| Column | Quick Column | `QS3DDRAWCOLUMNADV` behavior |
| Slab | Quick Slab | `QS3DDRAWSLABADV` behavior |
| GlassWall | Quick GlassWall | `QS3DDRAWGLASSWALLADV` behavior |
| WallPier | Quick WallPier | `QS3DDRAWWALLPIERADV` behavior |
| StructuralWall | Quick StructuralWall | `QS3DDRAWSTRUCTWALLADV` behavior |
| Foundation | Quick Foundation | `QS3DDRAWFOUNDATIONADV` behavior |
| Door | Quick Door | `QS3DDRAWDOORADV` behavior |
| WallOpening | Quick WallOpening / Window | `QS3DDRAWOPENINGADV` / `QS3DDRAWWINDOWADV` behavior |

For `WallOpening`, Window remains the canonical WallOpening semantic category. Both dispatchers resolve Window deterministically as follows:

1. explicit `OpeningUsage=Window` wins;
2. for legacy Family data without `OpeningUsage`, dedicated `WindowHeightM` or `WindowSillHeightM` keys identify the Window authoring path;
3. otherwise the Family uses normal WallOpening authoring.

Unsupported categories fail closed with guidance to use their specialized workflow. The dispatchers do not invent Direct Draw behavior for Grid, Room, Stair, Railing, Earthwork, finishes, or other categories whose source/native lifecycle is different.

## Non-creating boundary

The dispatcher layer is **read-only / non-creating**:

- it uses `ProjectContextCoordinator.TryGetReadOnly(...)`;
- it requires an existing active Family;
- it never calls `GetOrCreate`;
- it never canonical-binds a project merely because the user invoked Quick/Advanced active-family draw;
- it never creates CAD, semantic state, audit state or native output by itself.

After dispatch, the chosen target command remains the sole owner of its established geometry-acquisition, preview-project freshness, prompt/default handling, semantic capture, scoped regeneration, ownership and rollback lifecycle. Therefore Advanced continues to inherit the existing same-ProjectId/ChangeVersion/projectless-preview freshness guards rather than implementing another confirmation model here.

The Workspace gesture layer intentionally calls the existing canonical `SetActiveFamily(...)` operation before dispatch. That is the same user-requested Family-selection mutation already used by Workspace authoring actions; it is not a geometry or project-bootstrap shortcut.

## Why this is not a second authoring engine

The active-family dispatcher contains no source geometry builder, no semantic capture implementation, no regeneration engine and no native builder. It only selects the already-supported Quick or Advanced method from the current active Family category.

The Workspace gesture layer likewise contains no category switch. It only sets the selected Family active and sends `QS3DDRAWACTIVE` or `QS3DDRAWACTIVEADV`.

This keeps the product direction simple:

```text
Family / Type = what you are drawing
Vẽ Nhanh = normal object
Vẽ tùy chỉnh = one-off exception
```

## Runtime qualification boundary

Source/static implementation is REMOTE_DONE. Exact BricsCAD V25 interaction remains part of the existing `LOCAL-008` Direct Draw qualification boundary.

Local proof should include:

1. no project / no active Family: both active-family commands return without creating/cache-binding a project or CAD state;
2. every supported active Family category dispatches to the same Quick/Advanced behavior as invoking that category command directly;
3. changing active Family in Workspace changes the next Quick or Advanced dispatch without requiring a Ribbon-command change;
4. WallOpening versus Window Family metadata dispatch is deterministic and Window still persists `OpeningUsage=Window` on created semantics;
5. ESC/cancel inside a delegated Quick or Advanced command leaves the same no-residue state as direct invocation;
6. Advanced prompt cancellation and preview-freshness refusal stay owned by the existing target `*ADV` command;
7. unsupported active categories fail closed without mutation;
8. Ribbon **Vẽ Nhanh** routes to `QS3DDRAWACTIVE` and remains usable across document switches without cross-DWG mutation;
9. Workspace Family double-click / `Ctrl+D` / context-menu Quick each activate exactly the selected live Family and launch one Quick command only;
10. `Ctrl+Shift+D` / context-menu **Vẽ tùy chỉnh** each activate exactly the selected live Family and launch one Advanced command only;
11. stale/reloaded Workspace Family rows still fail closed through the existing canonical `SetActiveFamily` boundary instead of dispatching against a stale Family;
12. repeated load/unload of the modeless Workspace does not accumulate duplicate key, double-click, or context-menu handlers.

Transient DrawJig preview, true continuous/repeated authoring and native editor lifecycle remain LOCAL_ONLY under `LOCAL-008`; these dispatchers/gestures do not claim those runtime behaviors are complete.

GitHub Actions remain manual-only under `CI_POLICY.md`; this source/docs batch does not authorize workflow dispatch.
