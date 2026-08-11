# Work Claim: Active Family Canonical No-Op

- Status: `ACTIVE`
- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-12
- Mode: Remote source-safe
- Baseline main SHA: `3df37e80e4ee2c994cea6c55c3839c533bab272d`
- Scope: preserve true no-op semantics when persisted `ActiveFamilyId` uses padded/case-varied formatting but resolves to the same canonical Family selected by `SetActive(...)`.

## Reserved files

- `src/QS3D.Core/Domain/ProjectFamilyActivationService.cs`
- `tests/QS3D.Core.SmokeTests/ProjectFamilyActivationCanonicalNoOpSmoke.cs`
- `tests/QS3D.Core.SmokeTests/ProjectFamilyActivationCanonicalNoOpSmokeRegistration.cs`
- `docs/agent-work-claims/2026-08-12-0045-chatgpt-web-gpt56sol-active-family-canonical-noop.md`

## Defect evidence

`GetActive(...)` and `ClearIfMissing(...)` already resolve persisted active-family identity through trimmed canonical lookup, while `SetActive(...)` compares the raw persisted metadata string directly with `family.Id`. A value such as `"  f-beam  "` therefore resolves to the same Family but `SetActive(project, "F-BEAM")` still calls `Touch()` and rewrites metadata. That is a false mutation for a canonical-equivalent selection.

## Boundaries

- Core/Domain only; no BricsCAD/native/UI changes.
- Preserve exact persisted metadata for canonical-equivalent no-ops; do not repair formatting as a side effect.
- Preserve real active-family changes, missing-family rejection, and `ClearIfMissing(...)` behavior.
- No GitHub Actions dispatch.

## Validation plan

- Resolve the current persisted active-family reference canonically before deciding whether `SetActive(...)` is a no-op.
- Add isolated smoke coverage proving padded/case-varied same-Family activation preserves raw metadata and `ChangeVersion`, while switching to a different Family still mutates exactly once.
- Review exact source/test diff through GitHub connector.
- Do not claim BricsCAD V25 runtime validation or remotely executed smoke pass unless actually available.
