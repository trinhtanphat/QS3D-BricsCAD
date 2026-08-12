# Work claim — Project name overflow atomicity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-project-name-overflow-atomicity-20260812-0955`
- Registered: `2026-08-12T09:55:00+07:00`
- Baseline main SHA: `f490a146cfc0e1889da993edc03f0cd1acd18d19`

## Confirmed defect

PR #722 / merge `503edb6e2bdee487c5d45f3849fa4e5ad5582f6f` made public `ProjectState.Name` changes participate in persistence freshness, but the setter currently assigns `_name = next` before calling `Touch()`. When `ChangeVersion == long.MaxValue`, `Touch()` throws `OverflowException` after `_name` has already changed. The mutation therefore violates all-or-nothing semantics: the visible project name changes while `ChangeVersion` and `UpdatedUtc` remain at the pre-call persistence state.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectState.cs`
- focused overflow regression in `tests/QS3D.Core.SmokeTests/ProjectNameFreshnessSmoke.cs`
- this claim file

Make real Name changes preflight the revision increment before mutating `_name`, while preserving canonical-equivalent no-op behavior, blank-input validation, exactly-one revision increment on success, snapshot restore semantics, and constructor behavior. Do not redesign other ProjectState setters or persistence policy.

## Completion

Complete only after source + focused regression are on current `main`, exact SHAs are recorded here, and this claim is marked `COMPLETED`. No GitHub Actions, local .NET build/smoke execution, or BricsCAD V25/V26 runtime qualification is claimed by this remote lane.