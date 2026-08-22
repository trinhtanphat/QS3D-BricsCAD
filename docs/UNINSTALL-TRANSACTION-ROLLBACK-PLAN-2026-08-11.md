# QS3D V25 uninstall — transaction/rollback plan

## Goal

Prevent direct uninstall from leaving a half-uninstalled QS3D state when file or registry mutation fails.

## Transaction model

The uninstall touches two independent persistence surfaces: the per-user QS3D install directory and one or more BricsCAD V25 DemandLoad registry trees. Recursive deletion is not atomic, so the canonical install directory must first be moved to a same-parent quarantine name. Registry removals are then performed from complete snapshots. The canonical state is committed once all approved registry removals succeed; physical deletion of the quarantined files becomes post-commit cleanup.

## Implementation sequence

1. Keep the existing all-BricsCAD-closed check and acquire the existing per-user update/install/uninstall mutex.
2. Resolve/validate the install directory exactly as today when files are eligible for removal.
3. Discover selected V25/language `Applications\QS3D` keys without mutating them.
4. Add recursive registry snapshot/restore helpers that preserve every value name, value data, `RegistryValueKind`, and child key (including `Commands`).
5. Evaluate `ShouldProcess` for each selected registry key and for file removal. Only approved surfaces enter the mutation plan.
6. If approved file removal targets an existing validated directory, move it to a unique `.qs3d-uninstall-<guid>` sibling before registry mutation. A staging failure stops with canonical files/registry untouched.
7. Snapshot every approved existing registry tree before mutation. During delete, add its snapshot to rollback tracking before `Remove-Item` so a partially failing delete is covered.
8. On any pre-commit mutation failure, restore registry snapshots in reverse order and move quarantine back to the canonical path. Preserve the original error; emit rollback failures only as warnings.
9. After registry removals complete, treat the uninstall as committed. Remove quarantine recursively with `-ErrorAction Stop`; if cleanup fails, warn and leave the noncanonical unregistered quarantine residue for manual cleanup rather than rolling back a completed logical uninstall.
10. `-KeepFiles` never stages/removes files but still receives transactional registry rollback.

## Static regression

Add `scripts/preflight-uninstall-transaction.py` requiring:

- recursive registry tree snapshot and restore with value kinds;
- target discovery before mutation;
- quarantine `Move-Item` before any registry `Remove-Item`;
- rollback tracking before each registry delete;
- reverse registry restore + quarantine move-back on failure;
- original failure rethrow after rollback warnings;
- post-commit quarantine cleanup warning semantics;
- preserved `ShouldProcess`, `-KeepFiles`, safe custom-path/package identity, shared update mutex and no forced process termination.

## Runtime boundary

Actual Windows registry/file-lock fault injection, ACL failures, quarantine cleanup residue, `-WhatIf`, `-KeepFiles`, multi-language rollback and customer-like uninstall remain part of `LOCAL-009`. This source lane does not claim those runtime scenarios PASS.