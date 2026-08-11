# Work claim — Revision element payload canonical integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-revision-payload-20260811-2300`
- Registered: `2026-08-11T23:00:00+07:00`
- Baseline main SHA: `0c32164f96fdc7d9fa4d7abc9dd855fcc6c49826`
- Priority: P2 source-proven regression hardening

## Reserved scope

Finish the source-safe Revision element invariant alignment between public/in-memory snapshots and `RevisionSnapshotStore.Save`. The persistence boundary requires canonical category text, canonical property/quantity keys, finite quantities, and canonical unique source/dependency lists. `RevisionService.Capture` can still copy non-canonical map keys from directly mutable `ProjectElement` dictionaries, while `RevisionService.Compare` can bypass payload validation entirely for Added/Removed elements and currently normalizes malformed dependency lists during field comparison. `QuantityRevisionReport.Build` also accepts non-canonical quantity keys when they happen to compare equal.

## Expected surfaces

- `src/QS3D.Core/Revisions/RevisionService.cs`
- `src/QS3D.Core/Revisions/QuantityRevisionReport.cs`
- `tests/QS3D.Core.SmokeTests/RevisionRegressionSmoke.cs`
- this claim file for close-out

## Explicit exclusions

- No revision XML schema/version or `RevisionSnapshotStore` changes.
- No Revision UI/code-behind or BricsCAD coordinator changes.
- No changes to `ProjectElement` dictionary/list ownership architecture.
- No quantity calculation/rule semantics changes.
- No GitHub Actions dispatch or workflow edits.

## Validation plan

- Verify claim reachability from current `main`, then re-fetch exact source/test blobs before writes.
- During capture, reject non-canonical property/quantity keys before emitting a snapshot; continue existing finite quantity and canonical source-handle behavior.
- During compare indexing, validate category canonicality, property/quantity key canonicality, finite quantity values, and source/dependency list canonicality/uniqueness so Added/Removed elements cannot bypass the snapshot contract.
- In `QuantityRevisionReport.Build`, reject non-canonical quantity keys and non-finite values at index time even when equal values would otherwise produce no rows.
- Add deterministic smoke coverage for direct mutable map keys and malformed manually supplied Added/Removed snapshot payloads.
- Source/static readback plus committed smoke coverage only; no local .NET/BricsCAD/Actions PASS claim.

## Coordination

The preceding Revision Core claims are completed. Recent Revision UI/read-only claims are completed and disjoint. If a newer active claim reserves these exact Core Revision files before implementation, stop and re-scope.

## Completion condition

Revision capture/compare/report cannot silently accept element payload forms that revision persistence rejects, focused regression coverage is committed on current `main`, current source is re-read, and this claim is marked `COMPLETED` with exact SHAs and actual validation scope.
