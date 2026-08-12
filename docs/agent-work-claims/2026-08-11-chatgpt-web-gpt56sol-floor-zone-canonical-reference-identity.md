# Work claim — Floor/Zone canonical reference identity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-floor-zone-canonical-reference-identity`
- Registered: `2026-08-11T22:43:00+07:00`
- Completed: `2026-08-11T22:49:00+07:00`
- Baseline main SHA observed: `be1befd7ee8e4026bedf497c1bb13abfc26240df`
- Priority: P1 — `ProjectElement.FloorId` / `ZoneId` and project active Floor/Zone ids are mutable strings. Previous Floor/Zone safety paths compared some values raw, so recoverable padded/case-varied references could disappear from update/reference/delete checks or bypass active-item deletion protection.

## Implemented

- `93ab3a5bcc81337c9cea6f7a4d4657105d65f2ec` — `ProjectFloorService.Delete()` now canonicalizes `ActiveFloorId` before active-floor protection; `ReferencesFloor()` canonicalizes the direct mutable `ProjectElement.FloorId` and the requested Floor id; `ReferencesVerticalLevel()` canonicalizes its requested id while retaining the existing trimmed Bottom/Top property helper.
- `f1bdc9042d4712214df354758696c7fad0a9be45` — `ProjectZoneService` now routes update/reference/delete membership through `ReferencesZone()`, which trims mutable `ProjectElement.ZoneId`, and `Delete()` canonicalizes `ActiveZoneId` before mutation.
- `5bccb132a11babd4d5b69ca13ecf6f34d9a374f0` — added deterministic smoke coverage for padded/case-varied Floor and Zone relations, Floor/Zone update dirtying, reference counting, referenced-delete refusal without mutation, and padded active Floor/Zone delete refusal.
- `f11bd692d9be6601a4768b5b2ed7b6c7037d8876` — module-registers the focused smoke without depending on the shared smoke-registration file.
- `a4242d4cb4a4fcee742fee3925a3e8e03ddb4f5c` — added `scripts/preflight-project-floor-zone-canonical-reference.py`, requiring trimmed active/reference identities and rejecting the previous raw safety comparisons.

## Preserved contracts

- No Floor/Level/Zone WPF or Workspace UI, `ProjectElement` setter rewrite, persistence migration, quantity/CAD/Ribbon/updater/release behavior changed.
- Existing vertical Bottom/Top Level property canonicalization remains in `Property()`; no new level semantics were invented.
- Assignment behavior remains unchanged: an explicit assignment may still canonicalize a padded current relation as a mutation.

## Validation

- Re-fetched current Floor source and confirmed active deletion uses trimmed `ActiveFloorId`, `ReferencesFloor()` trims both direct `FloorId` and requested id, and the deletion guard runs before `project.Touch()`.
- Re-fetched current Zone source and confirmed Update/ReferenceCount/Delete share `ReferencesZone()`, mutable `ZoneId` is trimmed, and active deletion trims `ActiveZoneId` before mutation.
- Re-fetched the smoke file and confirmed all four Floor/Zone reference/active scenarios are present.
- `a4242d4cb4a4fcee742fee3925a3e8e03ddb4f5c` is an ancestor of later concurrent `main`; the final comparison after that commit showed no later edits to this lane's source/test/preflight files.
- No GitHub Actions workflow was dispatched and no BricsCAD V25 runtime PASS is claimed; these are deterministic Core identity/safety fixes.

## LOCAL_ONLY disposition

- None added.

## Completion evidence

Floor/Zone update, reference counting and deletion safety now agree on canonical identities for mutable direct relations and active ids, closing the same recoverable-padding class of integrity defect already fixed for Family references. Final implementation/preflight tip for this lane: `a4242d4cb4a4fcee742fee3925a3e8e03ddb4f5c`.
