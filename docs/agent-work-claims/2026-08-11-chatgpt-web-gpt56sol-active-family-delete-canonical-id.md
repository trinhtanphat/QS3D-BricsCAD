# Work claim — Active Family deletion canonical-ID guard

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-active-family-delete-canonical-id`
- Registered: `2026-08-11T22:29:00+07:00`
- Completed: `2026-08-11T22:36:00+07:00`
- Baseline main SHA observed: `f24e8ce4e936365126887b7856a53f034de24175`
- Priority: P1 — `ProjectFamilyActivationService.GetActive()` canonicalizes a persisted `ActiveFamilyId` by trimming it, while the previous `ProjectFamilyService.Delete()` compared the raw metadata value and could therefore delete the Family that read paths still considered active.

## Implemented

- `dac52feddffdf186190dfb7a469e580accea0cb5` — `ProjectFamilyService.Delete()` now trims the persisted `ActiveFamilyId` before the existing case-insensitive comparison, and the guard still executes before `project.Touch()` or removal.
- `5743574ff99791c7625e8e630b6fd0045f3cf79b` — added deterministic smoke coverage for padded active IDs, case-varied padded IDs, rejected-delete non-mutation, and successful deletion of a genuinely inactive Family.
- `b81a19083852ca5379e3f828cf48da059833ef80` — module-registers the focused smoke without touching shared registration surfaces.
- `caccb67982d751ad0c827199a7d8a6bab6ec79cf` — added `scripts/preflight-project-family-active-delete-canonical.py`, requiring trim + case-insensitive active identity before mutation and rejecting the previous raw comparison.

## Preserved contracts

- No Workspace/Family UI, Family usage badge, Right Panel, quantity settings/rules, persistence/session recovery, CAD/native source, Ribbon, updater or release behavior changed.
- No second activation key or alternate Family model was introduced.
- Rejected active deletion still preserves the Family, `ChangeVersion`, and original metadata text; successful inactive deletion retains the existing mutation behavior.

## Validation

- Re-fetched current `main` after implementation and confirmed the canonical comparison is present in `ProjectFamilyService.Delete()` before `project.Touch()`.
- Re-fetched the focused smoke and confirmed all three scenarios are present, including read parity through `ProjectFamilyActivationService.GetActive()`.
- `caccb67982d751ad0c827199a7d8a6bab6ec79cf` is an ancestor of later concurrent `main`; subsequent commits did not touch this lane's source/test/preflight files in the final comparison.
- No GitHub Actions workflow was dispatched and no BricsCAD V25 runtime claim is made; this is a CAD-independent Core invariant fix.

## LOCAL_ONLY disposition

- None added. The defect and its regression contract are deterministic Core behavior.

## Completion evidence

Family read/delete paths now agree on canonical `ActiveFamilyId` identity, so padded recoverable metadata cannot bypass active-Family deletion protection. Final implementation/preflight tip for this lane: `caccb67982d751ad0c827199a7d8a6bab6ec79cf`.
