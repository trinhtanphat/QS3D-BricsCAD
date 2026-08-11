# Work claim — Family member canonical reference guard

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-family-member-canonical-reference`
- Registered: `2026-08-11T22:37:00+07:00`
- Completed: `2026-08-11T22:40:00+07:00`
- Baseline main SHA observed: `08d065ac766fc23c19df2d3d4a4ecba232c41c3a`
- Priority: P1 — `ProjectElement.FamilyId` is publicly mutable, while the previous `ProjectFamilyService.ResolveFamilyMembers()` compared its raw text to a canonical Family id. Recoverable padded relations could therefore disappear from Family reference counting/property propagation/deletion safety.

## Implemented

- `f50ca9caba58bd9c956cc75f236698a52ffcc0cd` — `ResolveFamilyMembers()` now trims nullable `ProjectElement.FamilyId` before the existing case-insensitive Family-id comparison.
- `29bbffe36ad42d000ad93c573bff36b7c49166d9` — added deterministic smoke coverage for padded and case-varied relations, deletion rejection without project mutation, and correct exclusion/deletion of an unrelated Family.
- `07f0418176c8110cb43856edda4ffa8dbab3a4ef` — module-registers the focused Core smoke.
- `9d59a61e4dfdc0ff34cb162b1b6a0cba3f764bcb` — added `scripts/preflight-project-family-member-canonical-reference.py`, requiring canonical relation matching inside `ResolveFamilyMembers()` and rejecting the previous raw comparison.

## Preserved contracts

- No `ProjectElement` relation setter rewrite, persistence migration, Workspace/Family UI, quantity settings/rules, CAD/native source, Ribbon, updater or release behavior changed.
- Duplicate semantic-element detection and Family assignment semantics remain unchanged.
- The earlier active-Family metadata deletion guard remains intact.

## Validation

- Re-fetched current `main` and confirmed `ResolveFamilyMembers()` uses `(element.FamilyId ?? string.Empty).Trim()` with `StringComparison.OrdinalIgnoreCase`.
- Current `main` immediately after the source/test/preflight batch had `9d59a61e4dfdc0ff34cb162b1b6a0cba3f764bcb` as the parent of the next concurrent commit, confirming this lane was integrated without overwriting concurrent history.
- Focused smoke/preflight source is present and module-registered. No GitHub Actions workflow was dispatched and no BricsCAD V25 runtime claim is made; this is CAD-independent Core behavior.

## LOCAL_ONLY disposition

- None added.

## Completion evidence

Family reference count, property propagation and deletion safety now agree on canonical Family relation identity even when recoverable runtime state contains padded/case-varied `FamilyId` text. Final implementation/preflight tip for this lane: `9d59a61e4dfdc0ff34cb162b1b6a0cba3f764bcb`.
