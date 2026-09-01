# Multi-Region rebar command failure redaction — V25 qualification

Status: SOURCE_READY / LOCAL_ONLY_RUNTIME / NO_RESULT

Issue: #5244
Lane-Key: `issue-5244`

This runbook qualifies the user-visible failure and post-commit UI boundary for the production Multi-Region Slab/Foundation rebar commands. Hosted/static CI may qualify source shape and V25 compilation, but it must never be reported as licensed BricsCAD runtime PASS.

## Source contract

The command layer must preserve the established native lifecycle:

- selection is acquired before mutation admission;
- an existing project is read through `TryGetReadOnly` and its `ProjectId` / `ChangeVersion` are captured;
- `ExistingProjectMutationContext.Require` reopens the same project for mutation and exact snapshot freshness is rechecked;
- `SlabFoundationMultiRegionMeshSolidBuilder.BuildSlab` / `BuildFoundation` retain native generation, ownership, topology validation, rollback, metadata and transaction authority;
- Health remains read-only and never creates/replaces generated geometry;
- caught host/native exception detail is not copied to Palette or Editor surfaces;
- after a successful builder return, Refresh/Regen/status are independent best-effort operations. Their failure may produce only a stable warning that native update already completed.

## LOCAL_ONLY matrix

Bind every row to one exact pushed candidate SHA, adapter/Core ProductVersion, adapter/Core SHA-256 and licensed BricsCAD V25 host identity. Use disposable/sanitized drawings and restore pre-state after each bounded run.

| ID | Scenario | Required result |
|---|---|---|
| MR01 | `QS3DSLABREBAR3DMULTI`, no selection | Stable selection guidance; no project/native mutation. |
| MR02 | `QS3DFOUNDATIONREBAR3DMULTI`, no selection | Stable selection guidance; no project/native mutation. |
| MR03 | Slab command with no existing QS3D project | Stable BLOCKED result; command must not bootstrap a project. |
| MR04 | Foundation command with no existing QS3D project | Stable BLOCKED result; command must not bootstrap a project. |
| MR05 | Valid Slab multi-region outer/hole/disconnected selection | Canonical native bars generated/replaced with expected ownership/manifests; no raw host detail. |
| MR06 | Valid Foundation multi-region outer/hole/disconnected selection | Canonical native bars generated/replaced with expected ownership/manifests; no raw host detail. |
| MR07 | Unsupported/ambiguous topology or stale project snapshot | Fail closed with stable operation failure; no partial semantic/native publication and no raw exception text. |
| MR08 | Force Palette refresh/status failure after successful Slab native commit | Native output remains committed; Editor shows only stable post-commit UI warning. |
| MR09 | Force Editor Regen failure after successful Foundation native commit | Native output remains committed; status/editor reporting does not reclassify operation as native failure. |
| MR10 | `QS3DMULTIREBARHEALTH` on valid project | Read-only summary/details; no project revision/native mutation. |
| MR11 | Health presentation failure (Palette or Editor write) | Inspection remains read-only; presentation failure is fail-isolated and no raw exception detail is exposed. |
| MR12 | Save/cold reopen + second-DWG isolation after valid generated output | Ownership/health remain correct for the original DWG; no cross-document state bleed. |

## Evidence rules

For each executed row record only sanitized evidence: exact Git SHA, ProductVersion, relevant binary hashes, BricsCAD version, row verdict, bounded observations, cleanup/process residue and whether the drawing/project was modified as expected. Do not publish private drawing content, file-system secrets, stack traces or raw exception messages.

A row is `LOCAL_PASS` only when it was actually executed on the exact bound licensed candidate. Source guards, Core smoke, V25 compilation, branch CI and protected PR CI remain `REMOTE_SAFE` evidence only.
