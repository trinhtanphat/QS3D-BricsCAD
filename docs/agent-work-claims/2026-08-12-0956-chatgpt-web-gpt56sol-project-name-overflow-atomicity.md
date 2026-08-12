# Work claim — ProjectState Name ChangeVersion-overflow atomicity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-project-name-overflow-atomicity-20260812-0956`
- Registered: `2026-08-12T09:56:00+07:00`
- Baseline main SHA: `2808d90412f298dee0e008a7806a7e898c360366`
- Priority: P1 — close an atomicity regression in the just-merged Project Name freshness fix.

## Confirmed defect

The completed Project Name freshness lane made a real `ProjectState.Name` change call `Touch()`, but currently assigns `_name = next` before `Touch()`. `Touch()` computes `checked(ChangeVersion + 1L)` and can throw when `ChangeVersion == long.MaxValue`. At that boundary the setter throws after the persisted Name has already changed, leaving a partially-mutated project with unchanged freshness. Core mutation services normally call `project.Touch()` before their non-throwing field assignments for exactly this fail-before-mutate property.

## Reserved surfaces

- `src/QS3D.Core/Domain/ProjectState.cs` — Name setter statement order only
- `tests/QS3D.Core.SmokeTests/ProjectNameOverflowAtomicitySmoke.cs` — new focused regression
- this claim file

## Intended fix

- Keep validation and canonical-equivalent no-op behavior unchanged.
- For a real name change, call `Touch()` first; assign `_name` only after the checked freshness increment succeeds.
- Preserve the previously merged one-revision persistence freshness behavior in normal states.
- Add focused smoke that seeds `ChangeVersion = long.MaxValue` through the existing internal persistence-restoration method via reflection, then proves rename throws `OverflowException` without changing Name/ChangeVersion/UpdatedUtc; include a normal rename control.

## Coordination

This is a narrow follow-up to completed PR #722. It does not alter QSDB changeVersion parsing, ProjectStateSnapshot semantics, selection build work or any native/UI file.

## Validation boundary

Committed deterministic Core smoke coverage plus exact source/diff review. No GitHub Actions dispatch; no licensed BricsCAD V25/V26 runtime PASS claimed.
