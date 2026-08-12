# Work claim — Family target operations global duplicate integrity

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-family-global-duplicate-integrity-20260812-0858`
- Registered: `2026-08-12T08:58:00+07:00`
- Completed: `2026-08-12T09:00:00+07:00`
- Baseline main SHA: `a0baa00cef3f16df671539af50329bcbcd4ee8e9`
- Claim commit: `bf37cb1f7951ebb1239ee65c41a2941aa4429c0f`
- Source fix commit: `44779226f6fe49129cbc82c830b79232cc80426f`
- Focused smoke commit: `d5509b126a59b7753c459c4b7c0ee0f137ffed80`
- Priority: P1 — Family target operations must reject globally ambiguous Family identity state.
- Task Key: `CORE-FAMILY-TARGET-OPS-GLOBAL-DUPLICATE-ID`

## Confirmed defect

`ProjectFamilyService.FindRequired(...)` delegated to `ProjectState.FindFamily(targetId)`, which detects duplicate identities only when they match the requested target. An unrelated duplicate pair such as `F1`/`f1` could therefore coexist with unique `F2`, allowing Family operations on `F2` to proceed on globally invalid identity state.

## Implemented contract

- `ValidateUniqueFamilyIds(...)` checks all existing non-null Family IDs case-insensitively.
- `Create(...)` reuses the helper after its existing null-family guard.
- `FindRequired(...)` validates global identity before target resolution, covering Duplicate source resolution, Rename, SetProperty, RemoveProperty, Assign, Delete and ReferenceCount.
- Existing null behavior remains delegated to Create's guard / `ProjectState.FindFamily`; valid no-op, inheritance, category and assignment semantics remain unchanged.
- Floor/Zone services, Family UI, persistence/interchange and native BricsCAD code were not modified.

## Validation evidence

- Current `main` readback confirms Create and FindRequired share the global Family-ID validator.
- `ProjectFamilyGlobalDuplicateIntegritySmoke` is auto-registered and exercises Duplicate, Rename, SetProperty, RemoveProperty, Assign, Delete and ReferenceCount against `F1`/`f1` plus unique `F2`, proving no Family/element/revision/timestamp mutation on rejection.
- The same smoke preserves valid Rename, property inheritance/removal, Assign and ReferenceCount behavior.
- This connector-only session did not execute .NET smoke, GitHub Actions or licensed BricsCAD runtime tests.

## Completion

`COMPLETED`: target-based Family operations now fail closed on unrelated duplicate Family identities before mutation or result production.
