# QSDB preserved-backup generation fence

## Scope

This runbook covers `QsdbProjectStore.SavePreservingValidatedBackup`. It is a REMOTE_SAFE Core/Persistence contract; it does not claim licensed BricsCAD runtime qualification.

## Invariant

Recovery-safe primary publication may succeed only while the validated `.bak` remains the same canonical filesystem generation and the same QSDB project identity that was admitted before publication.

The method must:

1. resolve the canonical backup pathname and require it to exist;
2. open a held read fence with `FileShare.Read`, permitting readers while denying writer/delete replacement opens on supported Windows;
3. bind that held handle to the canonical backup pathname with `PersistencePathSafety.RequireExclusiveOpenStillBound` before trusting backup content;
4. validate the backup and require exact `ProjectId` equality before publishing the primary;
5. keep the held fence alive across `SaveCore(... ReplacePrimaryOnly ...)` and validation of the newly published primary;
6. re-bind the same held backup handle to the canonical pathname after publication;
7. parse the final backup and require exact `ProjectId` equality again before returning success.

A pathname-only precheck or a final parse-only check is insufficient because a non-cooperating writer could otherwise replace a valid backup with another valid QSDB generation between admission and return.

## Failure behavior

Generation drift, reparse redirection, missing backup, malformed backup, or project-identity mismatch fails closed. The implementation does not downgrade exact-generation checks on non-Windows platforms; `PersistencePathSafety` retains its supported-product Windows boundary.

The held backup fence does not replace the normal primary atomic-publication logic. It protects the separate promise made by `SavePreservingValidatedBackup`: the already validated backup is preserved while a repaired primary is published.

## Deterministic guard

Run:

```text
python scripts/preflight-qsdb-preserved-backup-generation-fence.py
```

The guard pins source ordering and fence lifetime:

`backup path -> held FileStream -> generation bind -> validated backup + identity -> primary SaveCore -> primary Load -> generation rebind -> final backup Load + identity`.

For protected merge authority, the exact current PR candidate must also pass repository-required `preflight` and `core` contexts under current CI policy. Static/hosted evidence must not be reported as BricsCAD `LOCAL_PASS`.
