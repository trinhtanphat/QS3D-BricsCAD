# QS3D project rollback failure matrix

Updated: 2026-08-25

Status: `REMOTE_DONE` for the bounded test-only rollback regression infrastructure in this batch. Native BricsCAD transaction rollback and exact-SHA runtime qualification remain `LOCAL_ONLY`.

## Purpose

WS-35 needs repeatable evidence that a failed semantic operation restores the **whole-project** state rather than a few manually selected fields. This batch adds a reusable smoke-test assertion helper plus a staged failure matrix around the existing `ProjectSemanticMutationExecutor` / `ProjectStateSnapshot` boundary.

The implementation deliberately adds **no production fault switch**. Failures are injected only inside the smoke-test delegate passed to the existing executor.

## Reusable whole-project assertion

`ProjectRollbackAssert` captures its expected baseline through `ProjectStateSnapshot.CreateDetachedCopy(...)` and compares the state that the canonical snapshot contract is responsible for preserving:

- schema, project identity/name and drawing context;
- active zone/floor;
- project `UpdatedUtc` and `ChangeVersion`;
- zone and floor catalogs;
- family identity/category/properties;
- element identity/category/family/floor/zone/drawing fingerprint;
- element source handles and dependencies;
- element properties and quantities;
- element dirty flags and `UpdatedUtc`;
- quantity rules;
- audit events, including actor/correlation metadata;
- project metadata.

Collection order and dictionary key spelling are compared strictly. That makes the helper useful for detecting a rollback that reconstructs approximately equivalent data but does not restore the persisted semantic state exactly.

## Staged matrix

`ProjectRollbackFailureMatrixSmoke` creates one representative project and injects failures at these semantic stages:

1. after project/catalog changes;
2. after element/source/dependency/property/quantity changes;
3. after quantity-rule, audit and metadata changes plus a `Touch()`;
4. inside pre-commit validation after every mutation stage completed.

For every failed run the test requires:

- the original injected exception to propagate;
- the mutation journal to contain `RollingBack` and `RolledBack`;
- no `Committed` phase;
- `ProjectRollbackAssert.Equivalent(...)` to match the detached baseline across the whole-project contract.

The helper also has a negative self-test: an intentional metadata drift must be detected instead of silently passing.

## Production boundary

No changes are required in `ProjectStateSnapshot` or `ProjectSemanticMutationExecutor` for this matrix. The smoke test exercises their existing public behavior.

There is no production environment variable, global flag, callback, fault injector or hidden stage hook. Test code simply throws from the mutation/pre-commit validation delegate at deterministic points.

This matters because fault infrastructure that leaks into production can become another mutation path and create its own lifecycle risk. WS-35 regression coverage should test production boundaries, not rewrite them around test controls.

## What this proves

Once executed successfully on a suitable source-validation host, the matrix gives repeatable evidence that the existing semantic executor restores the canonical in-memory project snapshot after failures at multiple mutation phases.

It does **not** prove native BricsCAD entity rollback. `ProjectStateSnapshot` owns semantic `ProjectState`; a BricsCAD `Transaction`/DocumentLock/native ownership operation has a separate runtime boundary.

## Regression gate

`scripts/preflight-project-rollback-failure-matrix.py` is auto-discovered by `scripts/preflight-all.py`. It checks that:

- the reusable assertion remains backed by `ProjectStateSnapshot.CreateDetachedCopy`;
- all canonical project-state areas remain covered by the assertion;
- the staged mutation and pre-commit validation failures remain in the smoke matrix;
- the matrix checks `RollingBack` / `RolledBack` and rejects `Committed`;
- the production snapshot/executor continue using their canonical capture/restore contract;
- no production fault switch is introduced into those two production files.

## Qualification boundary

Still `LOCAL_ONLY` / separate validation:

- BricsCAD V25 native transaction abort/rollback;
- DocumentLock failures and multi-DWG switching during mutation;
- CAD handle/object ownership rollback;
- generated geometry/rebar native cleanup after partial native work;
- WPF/modeless post-commit failure isolation on a licensed V25 host;
- private-DWG failure scenarios;
- exact-SHA NETLOAD/runtime qualification.

Those scenarios should reuse the same principle—capture the authoritative pre-operation state and assert exact restoration—but they cannot be claimed from this Core smoke matrix alone.

## LOCAL-011 executable handoff

Issue `#3905` closes the repository-side orchestration gap for the existing `LOCAL-011` queue item. A compatible local agent should **pull/sync the newest intended SHA and run one command**:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run-local-v25-local-011.ps1
```

The runner verifies a clean exact checkout, all four source-ready LOCAL-011 ancestors, and the canonical licensed V25 runtime baseline before starting one dedicated BricsCAD session. It records the 21 native/modeless/generated-replacement rows defined in `docs/LOCAL-011-NATIVE-QUALIFICATION.md`, writes fail-closed JSON evidence, never terminates an existing BricsCAD session, and never claims `LOCAL_PASS` on behalf of the local agent. Missing runtime capability/evidence remains `BLOCKED`/`NO_RESULT`; a product defect remains `FAIL` and returns to a normal source lane.