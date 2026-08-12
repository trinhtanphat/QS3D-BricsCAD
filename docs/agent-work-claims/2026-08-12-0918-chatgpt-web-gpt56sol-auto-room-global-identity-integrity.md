# Work claim — Auto Room family sync global identity integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-auto-room-global-identity-integrity-20260812-0918`
- Registered: `2026-08-12T09:18:00+07:00`
- Baseline main SHA: `63ac503d6ef73cb4aa88b8c1c2cf1c4628356704`
- Priority: P1 — prevent Auto Room Family synchronization from mutating a project whose semantic identity sets are already ambiguous.

## Confirmed defect

`AutoRoomLifecycle.SyncFamilyDefaults(...)` validated only the supplied Room and target Family through target-specific `ProjectState.FindElement/FindFamily` lookups. Unrelated duplicate semantic element IDs or unrelated duplicate Family IDs could therefore coexist while synchronization of a unique Room to a unique target Family still mutated Room properties, FamilyId, snapshot metadata and project revision.

## Implemented fix

- `SyncFamilyDefaults(...)` now runs the file's existing global semantic-element validation before ownership lookup.
- A minimal Family preflight rejects null entries and case-insensitive duplicate Family IDs globally.
- Dangling previous-Family fail-closed behavior, empty-Family bootstrap, canonical same-Family no-op semantics, override/default snapshot behavior and Auto Room topology/stale logic remain unchanged.
- Focused smoke covers unrelated duplicate elements, unrelated duplicate Families and a valid bootstrap synchronization control.

## Integration evidence

- Claim registration: `163d95a912962514386fcbe78068fd8e8d30861f`.
- Branch source commit: `a436035e76feddd5e669c2ba777a8569e1039ea4`.
- Source-only compare confirmed exactly +14 lines in `AutoRoomLifecycle.cs` and no incidental churn.
- Focused smoke commit: `a7a07c43f4e1b491e547920a51b90ee7fea03ff4`.
- Branch diff was exactly the reserved source plus the new 98-line smoke.
- Comparison from claim registration to PR base `9df191cf370b54ff1c1ab229e2e269994ab93ca6` showed 16 intervening commits and no reserved-path overlap.
- PR `#686` squash-merged at `f331cf1cfe6804983af6d0e040fbe4c366dceb91`.

## Validation boundary

Committed deterministic Core smoke coverage plus exact source/diff review. No GitHub Actions were dispatched and no licensed BricsCAD V25/V26 runtime PASS is claimed.
