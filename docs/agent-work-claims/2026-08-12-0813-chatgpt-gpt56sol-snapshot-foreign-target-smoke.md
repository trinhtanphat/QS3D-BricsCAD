# Work claim — ProjectStateSnapshot foreign-target identity smoke

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-snapshot-foreign-target-smoke`
- Registered: `2026-08-12T08:13:00+07:00`
- Last Updated: `2026-08-12T08:13:00+07:00`
- Baseline main SHA: `68517455c46f688a74f4a1d6632c9b93e8d4bb3a`
- Priority: follow-up regression coverage explicitly left uncommitted after prior optimistic-lock 409s
- Task Key: `CORE-PROJECT-STATE-SNAPSHOT-FOREIGN-TARGET-SMOKE`

## Scope

Add only the previously missing regression assertion proving that a snapshot captured from one canonical `ProjectState` does not inject captured `ProjectElement` object references when restored into a different `ProjectState` instance with the same `ProjectId`.

The production hardening already exists in `1994fcf9ea0ae7fbdf679e442c8d9775bd12d413`; this follow-up is test-only and must not modify `ProjectStateSnapshot` source.

## Completion condition

The focused smoke is committed on current `main`, read back after concurrent writes, and this claim is closed with exact evidence. No Actions/runtime qualification.