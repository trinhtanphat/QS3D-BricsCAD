# Work claim — Bulk Family assignment global identity integrity

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-bulk-family-global-identity-20260812-0959`
- Registered: `2026-08-12T09:59:00+07:00`
- Last Updated: `2026-08-12T10:03:00+07:00`
- Baseline main SHA: `15b4e5c37e0d8401d4f2cba1d9d75cbbb0df9802`
- Source fix SHA: `1cc96b26dc26af76db29211f454158577beaf0c0`
- Regression SHA: `17a37cadce0c89443a48c1ba18141447e94669f3`
- Priority: P1 — bulk assignment must not mutate a project whose Family identity space is already ambiguous.
- Task Key: `CORE-BULK-FAMILY-GLOBAL-IDENTITY`

## Confirmed defect

`BulkEditService.AssignFamily(...)` resolved the requested target through `project.FindFamily(familyId)` and snapshotted only that Family. Unlike `ProjectFamilyService.FindRequired(...)`, it did not validate global case-insensitive Family-ID uniqueness. A malformed project containing unrelated duplicate Families such as `F1` / `f1` plus a unique target `F2` could therefore bulk-assign `F2`, mutate element FamilyId/properties/dirty state and advance ProjectState even though QSDB persistence and canonical Family services reject the same Family collection as ambiguous.

## Completed implementation

- `AssignFamily(...)` now validates the entire Family collection before target lookup or property snapshot.
- Null Family entries, blank/non-canonical Family IDs, and case-insensitive duplicate Family IDs fail closed before target enumeration or semantic mutation.
- Valid Family assignment, inherited-property transfer, category checks, target freshness, duplicate Element-ID protection, mutation-executor rollback and existing no-op behavior remain unchanged.
- No unrelated bulk property-edit, persistence-schema, UI or native BricsCAD behavior was changed.

## Regression evidence

`tests/QS3D.Core.SmokeTests/BulkEditFamilyGlobalIdentitySmoke.cs` is auto-registered and covers:

- unrelated `F1` / `f1` duplicate Family IDs plus unique target `F2` are rejected;
- rejected assignment preserves element FamilyId, properties, dirty state and UpdatedUtc;
- rejected assignment preserves ProjectState ChangeVersion and UpdatedUtc;
- a valid unique `F2` assignment still binds the Family, inherits target properties, dirties the element and advances ChangeVersion exactly once.

Source and regression were read back directly from `main` after their commits.

## Validation boundary

No GitHub Actions were dispatched. No executable full smoke/build or licensed BricsCAD V25/V26 runtime PASS is claimed from this connector-only session.

## Completion condition

Completed: bulk Family assignment now fails closed on globally ambiguous Family identity before mutation while preserving normal valid assignment semantics.
