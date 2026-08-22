# Work claim — Family Activation global duplicate integrity

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-family-activation-global-duplicate-integrity-20260812-0901`
- Registered: `2026-08-12T09:01:00+07:00`
- Completed: `2026-08-12T09:03:00+07:00`
- Baseline main SHA: `074ebe1a79b7ead9e03aac777e013f6fcfb4b8a2`
- Claim commit: `7b16210497167156599a3e4f9080817511054182`
- Source fix commit: `7a89770ae933f45c51bfb6bab6ddc56cb9294b1a`
- Focused smoke commit: `20a3c46bf04e29015c1bd134596f39d7ec8aa9e4`
- Priority: P1 — active-Family state must not resolve or mutate through globally ambiguous Family identities.
- Task Key: `CORE-FAMILY-ACTIVATION-GLOBAL-DUPLICATE-ID`

## Confirmed defect

`ProjectFamilyActivationService.GetActive`, `SetActive`, and `ClearIfMissing` resolved only the active/target Family through `ProjectState.FindFamily(...)`. An unrelated duplicate pair such as `F1`/`f1` plus unique `F2` could therefore coexist while activation APIs returned/retained/switched `F2`, despite globally invalid Family identity state.

## Implemented contract

- All three activation APIs call `ValidateUniqueFamilyIds(...)` before active/target resolution.
- The helper checks non-null Family IDs case-insensitively and rejects duplicates with the same canonical duplicate-Family error used by ProjectFamilyService.
- Existing null-entry behavior remains delegated to `ProjectState.FindFamily` when a Family is actually resolved; blank/missing ActiveFamilyId behavior, canonical SetActive no-op, real active switch and missing-active cleanup remain unchanged.
- ProjectFamilyService, Family Manager UI, persistence/interchange, Floor/Zone services and native BricsCAD code were not modified.

## Validation evidence

- Current `main` readback confirms GetActive, SetActive and ClearIfMissing all run the duplicate-Family preflight.
- `ProjectFamilyActivationGlobalDuplicateIntegritySmoke` is auto-registered and proves all three APIs reject `F1`/`f1` plus unique `F2` without ActiveFamilyId/revision/timestamp mutation.
- The same smoke preserves canonical GetActive, padded/case-varied SetActive no-op, real F2 switch and missing-active cleanup with one revision advance.
- This connector-only session did not execute .NET smoke, GitHub Actions or licensed BricsCAD runtime tests.

## Completion

`COMPLETED`: Family activation APIs now fail closed on unrelated duplicate Family identities before returning or mutating active-Family state.
