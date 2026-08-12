# Work claim — transactional uninstall rollback

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T23:36:00+07:00`
- Completed: `2026-08-11T23:42:00+07:00`
- Baseline main SHA: `7137e373c1459db86a1184adbd81287e0a65ba47`
- Priority: owner-requested whole-repository audit; packaging mutation atomicity

## Verified defect

`scripts/uninstall-v25-autoload.ps1` removed selected BricsCAD `Applications\QS3D` DemandLoad registry trees first and only afterwards recursively deleted the install directory. There was no registry snapshot/rollback and no file staging boundary.

If recursive file removal failed, installed files could remain while DemandLoad registration had already been removed. If a later registry removal failed after earlier selected targets were deleted, registration could also be left partially removed across V25 language/version targets. The shared per-user update mutex prevented concurrent QS3D installer races but did not make the uninstall sequence atomic.

## Reserved scope

- `scripts/uninstall-v25-autoload.ps1`
- `scripts/preflight-uninstall-transaction.py`
- `scripts/preflight-update-cross-entry-lock.py` narrow compatibility update
- `docs/UNINSTALL-TRANSACTION-ROLLBACK-PLAN-2026-08-11.md`
- this claim file

## Completed contract

1. Selected existing DemandLoad targets are discovered before mutation; approved targets snapshot the full `Applications\QS3D` registry subtree including value kinds and nested `Commands` state.
2. For approved file-removing uninstall, the validated canonical install directory is first moved to a unique same-parent `.qs3d-uninstall-*` quarantine path before registry deletion.
3. Registry removal tracking is registered before each delete so a partially failing delete is included in rollback.
4. On pre-commit failure, quarantine is moved back to the canonical path before registry restore; registry snapshots are restored in reverse order. If canonical files cannot be restored, registry restoration is deliberately skipped to avoid recreating registrations to a missing loader. Rollback failures are warnings and the original error is rethrown.
5. After successful registry mutation the logical uninstall is committed. Quarantine deletion is best effort; cleanup failure warns and leaves only an unregistered noncanonical residue for manual deletion.
6. `-KeepFiles`, `-Force`, version/language filtering, `ShouldProcess`, package/custom-path identity checks, all-BricsCAD-closed precondition and the shared SID mutex remain intact.
7. `scripts/preflight-uninstall-transaction.py` guards the transaction ordering and rollback contract.
8. `scripts/preflight-update-cross-entry-lock.py` was reconciled both for this transactional uninstaller and for the previously completed bounded downloader, replacing its stale `Invoke-WebRequest` assumption with `Invoke-BoundedHttpsDownload`.

## Source/static evidence

- Source transaction commit: `0ab55e0e96e0a386bc76f5f8aedb432bf81fd43a`.
- Focused transaction gate: `ea89e12a4e5f7035ed84655e24db4db25b29f83f`.
- Cross-entry gate reconciliation: `310119c02f5176814788909187b410a38e69ecae`.
- Re-fetch confirmed current uninstaller blob `c82cec1e3990b16b2be33603898adea0275aee04` retains staging, recursive snapshots, rollback and quarantine cleanup semantics.
- Compare from `310119c02f5176814788909187b410a38e69ecae` to current `main` at close-out returned `behind_by: 0`; concurrent commits did not touch uninstall/gate surfaces.
- Native registry/file fault injection remains `LOCAL-009 / PENDING_LOCAL`; no remote runtime PASS is claimed.
