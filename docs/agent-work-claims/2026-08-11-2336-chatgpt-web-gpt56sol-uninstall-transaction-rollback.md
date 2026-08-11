# Work claim — transactional uninstall rollback

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T23:36:00+07:00`
- Baseline main SHA: `7137e373c1459db86a1184adbd81287e0a65ba47`
- Priority: owner-requested whole-repository audit; packaging mutation atomicity

## Verified defect

`scripts/uninstall-v25-autoload.ps1` currently removes selected BricsCAD `Applications\QS3D` DemandLoad registry trees first and only afterwards recursively deletes the install directory. There is no registry snapshot/rollback and no file staging boundary.

If recursive file removal fails, installed files can remain while DemandLoad registration has already been removed. If a later registry removal fails after earlier selected targets were deleted, registration can also be left partially removed across V25 language/version targets. The shared per-user update mutex prevents concurrent QS3D installer races but does not make the uninstall sequence atomic.

## Reserved scope

- `scripts/uninstall-v25-autoload.ps1`
- `scripts/preflight-uninstall-transaction.py` (new)
- `scripts/preflight-update-cross-entry-lock.py` only if a narrow compatibility update is required
- `docs/UNINSTALL-TRANSACTION-ROLLBACK-PLAN-2026-08-11.md` (new)
- this claim file

## Non-overlap / preservation

- Preserve close-all-BricsCAD precondition, shared Windows-SID update mutex, custom-path/package identity guard, `-KeepFiles`, `-Force`, `VersionKeys`, `LanguageKeys`, `ShouldProcess`, and no process force-kill.
- Do not edit updater/installer C#, final download lane, manifest generator, release workflow, semantic table, recognition, opening-host, GeneratedHandleOwnershipIndex or other active feature lanes.
- No Actions dispatch and no release publication.

## Intended contract

1. Resolve selected existing DemandLoad targets before mutation and snapshot each full `Applications\QS3D` registry subtree, including value kinds and nested `Commands` values.
2. For normal file-removing uninstall, after `ShouldProcess` approval rename the validated install directory to a same-parent unique quarantine path before deleting registry state. If staging rename fails, no registry mutation occurs.
3. Remove approved registry trees only after snapshots/staging are ready. Track every attempted removal snapshot so even a partially failing registry delete can be restored.
4. On any registry/mutation failure before commit, restore removed registry trees best effort and move the quarantined install directory back to the canonical path; preserve/rethrow the original failure and report rollback failures separately.
5. After registry mutation commits, delete the quarantine directory best effort. Cleanup failure must warn and leave only an unregistered, noncanonical quarantine residue; it must not convert a logically complete uninstall into a half-restored install.
6. `-KeepFiles` skips file staging/removal but still snapshots and rolls back partial registry removal failures.
7. `ShouldProcess` declines remain non-mutating for that surface and do not manufacture rollback state.

## Validation / release conditions

- Commit planning MD before implementation.
- Add an auto-discovered static regression proving stage-before-registry ordering, full registry subtree snapshot/restore, original-error preservation, reverse rollback, quarantine cleanup semantics and preserved mutex/path/ShouldProcess boundaries.
- Re-fetch exact source/gates and verify ancestry with `behind_by: 0` before close-out.
- Native registry/file fault injection remains `LOCAL-009 / PENDING_LOCAL`; no remote runtime PASS claim.
- Mark `COMPLETED` only after source + gate are on `main`.