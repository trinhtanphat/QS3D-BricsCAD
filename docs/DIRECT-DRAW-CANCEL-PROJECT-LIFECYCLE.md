# Direct Draw cancel / project lifecycle

Updated: 2026-08-11 (UTC+7)

Status: `SOURCE_IMPLEMENTED`; exact-SHA interactive BricsCAD V25 behavior remains `LOCAL_ONLY`.

This note refines the cancellation/atomicity contract in `docs/DIRECT-DRAW-WORKFLOW.md` for Direct Draw commands that can start from a clean DWG with no existing QS3D project.

## Source contract

The following authoring commands may read Family defaults from an **existing** project before their numeric parameter prompts, but they must not create/cache a new project merely to obtain defaults:

- `QS3DDRAWWALL`
- `QS3DDRAWBEAM`
- `QS3DDRAWSLAB`
- `QS3DDRAWCOLUMN`
- `QS3DDRAWGLASSWALL`
- `QS3DDRAWWALLPIER`
- `QS3DDRAWSTRUCTWALL`
- `QS3DDRAWFOUNDATION`
- `QS3DDRAWDOOR`
- `QS3DDRAWOPENING`
- `QS3DDRAWWALLREF`

Before the execution boundary these commands use `ProjectContextCoordinator.TryGetReadOnly(...)`. When there is no existing project/sidecar, the same guarded literal starter defaults remain available. `ProjectContextCoordinator.GetOrCreate(...)` is deferred until all geometry/reference acquisition and numeric prompts required for the operation have completed successfully.

This distinction matters because `GetOrCreate(...)` is an authoring mutation: on a clean DWG it can create and cache a default QS3D project. A user pressing ESC/Cancel during a thickness, height, offset, sill, clearance, width/depth or requested-length prompt must not create an otherwise empty project as a side effect.

## Cancel-before-execute expectation

On a clean disposable DWG with no existing QS3D project/sidecar, canceling any of the affected commands before its execution boundary must leave:

- no newly-created/cached QS3D project;
- no command-owned source LINE/POLYLINE;
- no new semantic Element;
- no generated/native output;
- no project-side audit/version change attributable to the canceled command.

Point/reference acquisition itself is editor input only until the command crosses its explicit source/project execution boundary. The existing rollback paths still govern failures that occur **after** execution begins and source/semantic/native work has started.

## Existing-project defaults

When a valid project already exists, pre-prompt default lookup remains read-only and should still honor the compatible active/preferred Family values used by the existing commands. This change is not permission to silently repair invalid Family numerics or to invent a project solely for defaults; malformed configured values continue to fail closed.

## Runtime qualification

Source/static ordering is guarded by `scripts/preflight-direct-draw-cancel-project-lifecycle.py`, but real cancel semantics depend on the BricsCAD editor and must be checked on the exact candidate SHA in licensed V25.

The local matrix is tracked in `docs/LOCAL-AGENT-INBOX.md` under `LOCAL-008`. It must include clean-DWG cancellation at each numeric prompt, verification that no QS3D project/source/semantic/native residue appears, and an existing-project pass proving Family defaults are still surfaced correctly.

No source/static result from this note constitutes `LOCAL_PASS`, NETLOAD proof, native Undo proof, or production-release evidence.
