# Work claim — Bulk Family assignment global identity integrity

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-bulk-family-global-identity-20260812-0959`
- Registered: `2026-08-12T09:59:00+07:00`
- Baseline main SHA: `15b4e5c37e0d8401d4f2cba1d9d75cbbb0df9802`
- Priority: P1 — bulk assignment must not mutate a project whose Family identity space is already ambiguous.
- Task Key: `CORE-BULK-FAMILY-GLOBAL-IDENTITY`

## Confirmed defect

`BulkEditService.AssignFamily(...)` resolves the requested target through `project.FindFamily(familyId)` and snapshots only that Family. Unlike `ProjectFamilyService.FindRequired(...)`, it never validates global case-insensitive Family-ID uniqueness. A malformed project containing unrelated duplicate Families such as `F1` / `f1` plus a unique target `F2` can therefore bulk-assign `F2`, mutate element FamilyId/properties/dirty state and advance ProjectState even though QSDB persistence and canonical Family services reject the same Family collection as ambiguous.

## Reserved scope

- `src/QS3D.Core/Services/BulkEditService.cs`
- `tests/QS3D.Core.SmokeTests/BulkEditFamilyGlobalIdentitySmoke.cs`
- this claim file

## Intended contract

- Preflight the entire `project.Families` collection for null entries, blank IDs and case-insensitive duplicate IDs before resolving/snapshotting the target Family.
- Fail before target enumeration or any element/project mutation when Family identity is ambiguous.
- Preserve valid assignment, inherited-property transfer, category checks, target freshness, duplicate Element-ID protection, mutation executor rollback and no-op behavior.
- Do not change ProjectFamilyService, UI/native BricsCAD, persistence schema or unrelated bulk property editing.

## Validation plan

Focused auto-registered Core smoke creates unrelated `F1`/`f1` duplicates and unique `F2`, attempts bulk assignment of `F2`, and requires failure with exact non-mutation of target FamilyId/properties/dirty state and ProjectState persistence state. A valid control proves normal Family assignment still transfers target inherited properties. Re-fetch source/claim before writes. No force-push, GitHub Actions dispatch, executable smoke PASS or licensed BricsCAD runtime qualification claim unless actually executed.
