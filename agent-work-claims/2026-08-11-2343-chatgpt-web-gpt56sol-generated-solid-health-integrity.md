# Work claim — generated solid runtime health integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-generated-solid-health-integrity`
- Registered: `2026-08-11T23:43:00+07:00`
- Baseline main SHA: `f90209264abc80b644aaff7f21ce93a8bfbbb0f0`
- Priority: source-verifiable runtime-health false-negative defect found during owner-requested continue-all audit

## Confirmed defect

`GeneratedSolidRuntimeHealthService.InspectGeneratedSolidOwnership(...)` silently skipped malformed `GeneratedSolidHandle` values, handle-resolution failures, invalid object ids, unreadable/erased entities, and handles resolving to a non-`Solid3d` entity. A project could therefore retain stale/corrupt generated-solid metadata while `QS3DHEALTHALL` reported no generated-solid issue.

## Reserved scope

Make generated-solid ownership inspection fail-visible while preserving the health path as strictly read-only:

- report malformed/non-hex generated-solid handles;
- report handles that cannot resolve to a valid database object;
- report generated objects that are unreadable, erased, or not `Solid3d`;
- retain the existing ownership-mismatch diagnostic for live `Solid3d` entities;
- do not repair, delete, restamp, save, touch project state, or open CAD objects for write from health inspection.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/Cad/GeneratedSolidRuntimeHealthService.cs`
- focused regression/preflight coverage under `scripts/`
- this claim file

## Excluded scope

- No changes to generated geometry creation/regeneration/cleanup write paths.
- No native table Build/Refresh/Remove semantics.
- No unrelated health provider rewrite.
- No licensed BricsCAD V25 runtime validation claim.
- No force-push or overwrite of concurrent agent work.

## Completed implementation

- Source fix: `56349def85997ce9bb6dcca1add4ba6bb2beb480` (`fix(health): surface corrupt generated solids`).
- Focused regression gate: `109218bcdd68526c5fc8414429964cad20a19476` (`test(health): pin generated solid integrity`).
- Gate: `scripts/preflight-generated-solid-runtime-health-integrity.py`; aggregate `scripts/preflight-all.py` discovers every `preflight-*.py` automatically, so no shared runner edit was required.

## Validation actually performed

- Re-fetched `GeneratedSolidRuntimeHealthService.cs` from current `main` after the gate commit; blob remained `3afc8199fcc2240346645cd9fb82e23dcbadcdcc` while `main` advanced concurrently.
- Verified fail-visible source coverage for malformed handle, handle-resolution exception/invalid ObjectId, unreadable/null DBObject, erased DBObject, non-`Solid3d`, and ownership mismatch.
- Verified ownership inspection still reads the CAD object with `OpenMode.ForRead` and the focused gate rejects `OpenMode.ForWrite`, `UpgradeOpen`, project `Touch`, mutation context, audit/save/get-or-create, erase, ownership stamping, and XData mutation tokens.
- Verified the existing provider-isolation gate remains separate; the new gate covers this lane's false-clean/read-only contract without changing shared provider semantics.
- Did not run or claim a full solution build, GitHub Actions PASS, or licensed BricsCAD V25 runtime PASS from the remote connector.

## Completion condition

Satisfied on the source contract: current `main` reports stale/corrupt generated-solid handle states instead of hiding them, regression coverage pins both fail-visible diagnostics and read-only ownership inspection, and this claim is closed as `COMPLETED`.
