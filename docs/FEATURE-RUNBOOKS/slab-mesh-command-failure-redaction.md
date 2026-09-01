# Slab Mesh command failure-isolation qualification

Issue: #5240  
Lane-Key: `issue-5240`  
Runtime class: `LOCAL_ONLY` for licensed BricsCAD V25 command/native/UI execution. Remote/static evidence is never `LOCAL_PASS`.

## Source contract

`QS3DSLABREBAR3D` retains the established selection-first/read-only-preview/existing-project-mutation flow, exact ProjectId + ChangeVersion + semantic-target-set freshness checks, and canonical `SlabMeshSolidBuilder.BuildSelected` native generation/ownership semantics. This carrier changes only the command/UI failure boundary:

- caught host/native exception detail is not copied to Palette or Editor output;
- build and health failures use stable operation-specific messages;
- Slab Mesh Health remains read-only and presentation failures cannot turn successful inspection into a mutation/failure claim;
- after `BuildSelected` returns, Palette refresh, Editor regen and status reporting are best-effort independent UI operations;
- a post-build UI failure reports only a stable warning and does not reclassify completed native work as a failed geometry transaction;
- issue diagnostics from `GeneratedSlabMeshHealthService` remain domain diagnostics, not caught host exception detail.

Deterministic remote acceptance includes the strengthened `scripts/preflight-slab-mesh-native.py` and focused auto-discovered `scripts/preflight-slab-mesh-command-failure-redaction.py`, followed by fresh exact-head protected `preflight` + `core`.

## Licensed V25 matrix — SMR01–SMR12

Use one exact authorized plugin artifact and disposable project drawings. Record ProductVersion/plugin SHA-256 and tested commit SHA before launch.

| Cell | Action | Required evidence |
| --- | --- | --- |
| SMR01 | Start V25, NETLOAD exact plugin, open a valid QS3D Slab project | Exact artifact identity; clean command registration |
| SMR02 | Select a valid Slab and run `QS3DSLABREBAR3D` | Existing native bars/ownership semantics preserved; stable success status |
| SMR03 | Run build with empty/unsupported selection | Stable guidance; no project creation or mutation from invalid selection |
| SMR04 | Change project semantic state between preview and mutation admission | Freshness guard refuses stale selection/target without native mutation |
| SMR05 | Force a controlled native/build exception before commit | Stable build failure; no raw exception message in Palette/Editor |
| SMR06 | Force Palette refresh failure after successful native build | Native result remains committed; stable post-build UI warning only |
| SMR07 | Force Editor regen/status presentation failure after successful build | Failure is isolated; completed native result is not reported as rolled back |
| SMR08 | Run `QS3DSLABREBARHEALTH` on healthy generated mesh | Read-only summary/details; no semantic/native mutation |
| SMR09 | Run Health with missing/corrupt generated handles | Domain health diagnostics remain visible and bounded/truncated as before |
| SMR10 | Force Palette/Editor presentation failure during Health | Inspection remains read-only; presentation failure does not escape or mutate |
| SMR11 | Repeat failed then successful Build/Health operations | No poisoned command/UI state; later operations remain usable |
| SMR12 | QSAVE, close drawing, fresh-process reopen, rerun Health | Native ownership/project persistence intact; no lifecycle residue |

## Verdict

`LOCAL_PASS` requires SMR01–SMR12 against the same exact artifact identity with sanitized evidence and cleanup. Native/runtime defects are `RUNTIME_FAIL` or `NO_RESULT`; hosted CI/source guards cannot be promoted to licensed runtime evidence.
