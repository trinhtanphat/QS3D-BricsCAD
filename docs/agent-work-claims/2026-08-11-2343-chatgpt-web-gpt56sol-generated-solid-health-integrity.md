# Work claim — generated solid runtime health integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-generated-solid-health-integrity`
- Registered: `2026-08-11T23:43:00+07:00`
- Baseline main SHA: `f90209264abc80b644aaff7f21ce93a8bfbbb0f0`
- Priority: source-verifiable runtime-health false-negative defect found during owner-requested continue-all audit

## Confirmed defect

`GeneratedSolidRuntimeHealthService.InspectGeneratedSolidOwnership(...)` silently skips malformed `GeneratedSolidHandle` values, handle-resolution failures, invalid object ids, unreadable/erased entities, and handles resolving to a non-`Solid3d` entity. A project can therefore retain stale/corrupt generated-solid metadata while `QS3DHEALTHALL` reports no generated-solid issue.

## Reserved scope

Make generated-solid ownership inspection fail-visible while preserving the health path as strictly read-only:

- report malformed/non-hex generated-solid handles;
- report handles that cannot resolve to a valid database object;
- report generated objects that are unreadable, erased, or not `Solid3d`;
- retain the existing ownership-mismatch diagnostic for live `Solid3d` entities;
- do not repair, delete, restamp, save, touch project state, or open CAD objects for write from health inspection.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/Cad/GeneratedSolidRuntimeHealthService.cs`
- focused regression/preflight coverage under `scripts/` if no equivalent current gate exists
- this claim file

## Excluded scope

- No changes to generated geometry creation/regeneration/cleanup write paths.
- No native table Build/Refresh/Remove semantics.
- No unrelated health provider rewrite.
- No licensed BricsCAD V25 runtime validation claim.
- No force-push or overwrite of concurrent agent work.

## Validation plan

- Re-fetch exact source after registration before editing.
- Verify every stale/corrupt handle state creates a deterministic `ModelHealthIssue` instead of silently continuing.
- Add a source regression gate that keeps the inspection path free from write/mutation APIs (`OpenMode.ForWrite`, `UpgradeOpen`, project `Touch`, mutation context, audit/save/get-or-create paths).
- Re-fetch final files from current `main` and inspect commit diffs.
- Do not claim GitHub Actions/full build/licensed runtime PASS without evidence.

## Completion condition

Current `main` reports stale/corrupt generated-solid handle states instead of hiding them, the inspection contract remains read-only, regression coverage prevents both false negatives and accidental health-time mutation, and this claim is closed as `COMPLETED` with exact commit SHAs and validation actually performed.
