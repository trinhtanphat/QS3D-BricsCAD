# QS3D Direct Draw — Active Family Quick Draw

Updated: 2026-08-11 (UTC+7)

## Goal

Reduce command-switching overhead after the modeler has already chosen the working **Family / Type**.

The stable entry point is:

```text
QS3DDRAWACTIVE
```

and the Quick Workflow Ribbon exposes it as **Vẽ Nhanh**.

The expected high-frequency loop becomes:

```text
choose Family / Type once
-> Vẽ Nhanh
-> acquire only the geometry required by that category
-> repeat the same Vẽ Nhanh entry point for the next active Family
```

This is complementary to the existing per-category primary commands. It does not remove them and does not replace any `*ADV` workflow.

## Workspace gestures

The Family / Type list now exposes the same stable entry point without forcing the modeler to move back to the Ribbon:

- **double-click a Family / Type** → make that row authoritative as the active Family and launch `QS3DDRAWACTIVE`;
- **Ctrl+D** while the Workspace has keyboard focus → launch `QS3DDRAWACTIVE` for the selected Family;
- Family context menu → **Vẽ Nhanh (Ctrl+D)**.

All three gestures reuse the same `WorkspaceViewModel.SetActiveFamily(...)` write already used by other Workspace authoring actions, then send the single `QS3DDRAWACTIVE` command. They do not duplicate per-category dispatch rules inside the Workspace.

The interaction intent is:

```text
Family list
-> select/double-click type
-> draw
```

instead of repeatedly moving between the Family palette and different Ribbon commands.

## Dispatch contract

`QS3DDRAWACTIVE` reads the existing active Family through `ProjectFamilyActivationService` and delegates to the already-guarded primary Quick Direct Draw command:

| Active Family category | Delegated primary command behavior |
|---|---|
| ArchitecturalWall | Quick Wall |
| Beam | Quick Beam |
| Column | Quick Column |
| Slab | Quick Slab |
| GlassWall | Quick GlassWall |
| WallPier | Quick WallPier |
| StructuralWall | Quick StructuralWall |
| Foundation | Quick Foundation |
| Door | Quick Door |
| WallOpening | Quick WallOpening or Quick Window according to canonical Window Family metadata |

For `WallOpening`, Window remains the canonical WallOpening semantic category. The dispatcher resolves Window deterministically as follows:

1. explicit `OpeningUsage=Window` wins;
2. for legacy Family data without `OpeningUsage`, dedicated `WindowHeightM` or `WindowSillHeightM` keys identify the Window authoring path;
3. otherwise the Family uses normal WallOpening authoring.

Unsupported categories fail closed with guidance to use their specialized workflow. The dispatcher does not invent Direct Draw behavior for Grid, Room, Stair, Railing, Earthwork, finishes, or other categories whose source/native lifecycle is different.

## Non-creating boundary

The dispatcher itself is **read-only / non-creating**:

- it uses `ProjectContextCoordinator.TryGetReadOnly(...)`;
- it requires an existing active Family;
- it never calls `GetOrCreate`;
- it never canonical-binds a project merely because the user clicked **Vẽ Nhanh**;
- it never creates CAD, semantic state, audit state or native output by itself.

After dispatch, the target primary command remains the sole owner of its established geometry-acquisition, preview-project, semantic capture, regeneration, ownership and rollback lifecycle. Therefore cancelling the target command keeps the same project-creation boundary as running that primary command directly.

The Workspace gesture layer does intentionally call the existing canonical `SetActiveFamily(...)` operation before dispatch. That is the same user-requested Family-selection mutation already used by Workspace authoring actions; it is not a geometry or project-bootstrap shortcut.

## Why this is not a second authoring engine

`QS3DDRAWACTIVE` contains no source geometry builder, no semantic capture implementation, no regeneration engine and no native builder. It only selects the already-supported primary command from the current active Family category.

The Workspace gesture layer likewise contains no category switch. It only sets the selected Family active and sends `QS3DDRAWACTIVE`.

This keeps the product direction simple:

```text
Family / Type = what you are drawing
Vẽ Nhanh = draw it
*ADV = one-off parameter override
```

## Runtime qualification boundary

Source/static implementation is REMOTE_DONE. Exact BricsCAD V25 interaction remains part of the existing `LOCAL-008` Direct Draw qualification boundary.

Local proof should include:

1. no project / no active Family: `QS3DDRAWACTIVE` returns without creating/cache-binding a project or CAD state;
2. every supported active Family category dispatches to the same primary Quick behavior as invoking that command directly;
3. changing active Family in Workspace changes the next **Vẽ Nhanh** dispatch without requiring a Ribbon-command change;
4. WallOpening versus Window Family metadata dispatch is deterministic and Window still persists `OpeningUsage=Window` on created semantics;
5. ESC/cancel inside the delegated primary command leaves the same no-residue state as direct invocation;
6. unsupported active categories fail closed without mutation;
7. Ribbon **Vẽ Nhanh** routes to `QS3DDRAWACTIVE` and remains usable across document switches without cross-DWG mutation;
8. Workspace Family double-click, `Ctrl+D`, and context-menu **Vẽ Nhanh** each activate exactly the selected live Family and launch one command only;
9. stale/reloaded Workspace Family rows still fail closed through the existing canonical `SetActiveFamily` boundary instead of dispatching against a stale Family;
10. repeated load/unload of the modeless Workspace does not accumulate duplicate key, double-click, or context-menu handlers.

Transient DrawJig preview, continuous/repeated authoring and native editor lifecycle remain LOCAL_ONLY under `LOCAL-008`; these gestures do not claim those runtime behaviors are complete.

GitHub Actions remain manual-only under `CI_POLICY.md`; this source/docs batch does not authorize workflow dispatch.
