# Level Picker managed-wrapper drift qualification

Issue: #5334  
Lane-Key: `issue-5334`  
Ownership-Key: `v25.floor-level.modeless-managed-wrapper-drift-v1`

## Boundary

This carrier hardens the BricsCAD V25 Level Picker when the host replaces the managed `Document` wrapper while the same native database remains live. It does **not** rebind the window to a replacement wrapper, alter `DocumentBoundWindowLifetime`, move CAD geometry, create replacement project state, or relax existing project/selection TOCTOU and rollback checks.

Remote CI may prove source contract, deterministic preflight, trusted references and V25 compilation. Managed-wrapper replacement and host lifecycle behavior require a licensed local BricsCAD V25 run. Until that run is recorded, acceptance remains **LOCAL_ONLY / NO_RESULT** and must not be reported as `LOCAL_PASS`.

## Prepared local matrix

| Cell | Local action | Required evidence |
| --- | --- | --- |
| FLW01 | Open a project-backed DWG, run `QS3DLEVELS`, activate and interact normally. | Picker remains open; floor read/edit path is unchanged. |
| FLW02 | With the picker open, activate a second DWG without replacing the original wrapper, then click a Level action. | Wrapper guard does not misclassify the other DWG as drift; existing active-drawing affinity refuses the action without mutation. |
| FLW03 | Reactivate the original DWG and use Refresh/selection inspection. | Original picker remains usable and resolves the original project. |
| FLW04 | Using the licensed wrapper-drift probe/host path, replace the managed wrapper for the same native database, then activate or send mouse/key input to the stale picker. | Stale picker consumes input and closes before a Level handler can mutate project/CAD state; stable warning requests reopening `QS3DLEVELS`. |
| FLW05 | After FLW04, run `QS3DLEVELS` again in that drawing. | New picker binds the current live wrapper and normal operations resume; no stale window remains. |
| FLW06 | Close the picker-owned DWG while another DWG remains live. | Picker closes safely; no stale wrapper access, orphan UI, or cross-DWG mutation. |
| FLW07 | Exercise host close/quit abort using the existing lifecycle probe path. | Existing `DocumentBoundWindowLifetime` abort recovery remains intact; no new native subscription behavior is introduced. |
| FLW08 | Replace/reload QS3D project state in the same live document while picker is open. | Existing project-affinity protection still closes/refuses stale semantic state; wrapper guard does not weaken it. |
| FLW09 | Change implied selection/semantic ownership between preview and assignment using the existing deterministic/manual race harness. | Existing selection TOCTOU validation refuses mutation; Floor/Level assignments remain atomic. |
| FLW10 | Create/update/activate/assign a Floor under normal same-wrapper operation, including an injected project-operation failure if available. | Existing `ProjectStateSnapshot` rollback behavior is preserved; no CAD source movement is introduced. |
| FLW11 | Repeat multi-DWG switch, same-native wrapper drift, close, and reopen cycles. | Exactly one usable picker for the live wrapper; no retained stale window or observable handler leak. |
| FLW12 | Inspect command line/status output through all failure cells, save, close and cold reopen. | Wrapper-drift warning is stable/redacted, contains no raw exception/stack detail, and project persistence remains coherent after reopen. |

## Remote deterministic validation

Run:

```text
python scripts/preflight-floor-level-wrapper-drift.py
```

The guard requires native database identity capture from the known-live constructor wrapper, live `DocumentManager` enumeration, disposed-wrapper rejection, native-identity match before managed-wrapper equality, fail-closed mouse/key consumption and window close, and a stable reopen warning. It also rejects project/CAD mutation APIs and raw host exception detail from the affinity-only partial.

## Qualification status

`FLW01–FLW12: LOCAL_ONLY / NO_RESULT` until executed against the exact candidate artifact in licensed BricsCAD V25. Remote source/build success is not licensed runtime evidence.
