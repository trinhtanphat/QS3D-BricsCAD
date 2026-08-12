# Work Claim: Active Family Canonical No-Op

- Status: `COMPLETED`
- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-12
- Completed: 2026-08-12
- Mode: Remote source-safe
- Baseline main SHA: `3df37e80e4ee2c994cea6c55c3839c533bab272d`
- Scope: preserve true no-op semantics when persisted `ActiveFamilyId` uses padded/case-varied formatting but resolves to the same canonical Family selected by `SetActive(...)`.

## Reserved files

- `src/QS3D.Core/Domain/ProjectFamilyActivationService.cs`
- `tests/QS3D.Core.SmokeTests/ProjectFamilyActivationCanonicalNoOpSmoke.cs`
- `tests/QS3D.Core.SmokeTests/ProjectFamilyActivationCanonicalNoOpSmokeRegistration.cs`
- `docs/agent-work-claims/2026-08-12-0045-chatgpt-web-gpt56sol-active-family-canonical-noop.md`

## Completed work

- `SetActive(...)` now resolves the currently persisted active-family reference through the same trimmed/case-insensitive project lookup used by the rest of the active-Family contract before deciding whether the selection is already active.
- Padded/case-varied metadata that resolves to the selected Family is a true no-op: raw metadata is preserved and `ProjectState.ChangeVersion` does not advance.
- Selecting a different Family still calls `Touch()` exactly once and stores that Family's canonical Id.
- Existing missing-Family rejection, `GetActive(...)`, and `ClearIfMissing(...)` behavior remain unchanged.
- Added isolated Core smoke coverage plus module-initializer registration without editing a shared smoke registry.

## Published commits / PR

- Claim-first commit: `8dc7fea5333a02493c16ae32df49842449a4e528`.
- Initial implementation branch commits: `58601675caba9bb9173fcd53698c9976d9fd6dd7`, `6e6e3b9a7c62802740c56f885faf938fefacfb42`, `9d3895e07dca8ee3a0cde7fbd81a70bad3558c79`.
- PR #588 was closed unmerged after synchronization picked up unrelated concurrent ancestry; it was not used for publication.
- Clean replacement PR #591 contained exactly the three reserved source/test files and was squash-merged.
- Published `main` squash SHA: `f6741e4a1421f7867ababb91e8265d26c6a1b605`.

## Validation notes

- Reviewed PR #591's exact three-file patch before merge.
- Re-read the current `ProjectFamilyService.Create(...)` contract used by the smoke fixture and confirmed it returns the created canonical `ProjectFamily` and touches once per creation before the smoke captures its baseline version.
- No GitHub Actions were dispatched.
- This Core-only batch does not claim BricsCAD V25 runtime validation or a remotely executed smoke-test PASS.

## Blocked dependencies

None.
