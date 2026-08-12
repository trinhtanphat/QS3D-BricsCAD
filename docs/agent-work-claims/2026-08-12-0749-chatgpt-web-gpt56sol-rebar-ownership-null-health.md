# Work claim — Generated Rebar Ownership health null-element fail-visible

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-rebar-ownership-null-health`
- Registered: `2026-08-12T07:49:00+07:00`
- Baseline main SHA: `7014868bd5ee1da9fda48f3c9ae90b35bc6fce47`
- Priority: P1 — standalone generated health must not silently treat malformed ProjectState entries as clean.
- Task Key: `CORE-REBAR-OWNERSHIP-NULL-HEALTH`

## Confirmed defect

`GeneratedRebarOwnershipHealthService.Inspect(ProjectState)` still executes `if (element == null) continue;`. A malformed project containing a null semantic element can therefore produce a false-clean result from this standalone provider. Newer health-provider lanes use a fail-visible contract: direct inspection rejects malformed null entries with `InvalidOperationException`, while `ComprehensiveModelHealthService.AddSafely(...)` converts the bounded failure into a stable Error-level `HEALTH_PROVIDER_FAILED` issue.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedRebarOwnershipHealthService.cs`
- one focused auto-discovered `scripts/preflight-*.py` regression gate
- this claim file

Do not modify generated rebar builders, ownership policy/index, rebar notation/fabrication, CAD runtime ownership, or `ComprehensiveModelHealthService`.

## Intended contract

- Direct Rebar Ownership health inspection throws `InvalidOperationException` on a null project element instead of silently skipping it.
- Valid projects retain existing handle/mode/ownership/legacy diagnostics.
- Composite health reuses existing `AddSafely` handling and remains fail-visible via `HEALTH_PROVIDER_FAILED` without aggregate changes.
- Inspection remains read-only.
- No GitHub Actions/build/release dispatch and no executable Core/full-build/BricsCAD runtime PASS claim from this remote lane.

## Completion condition

Standalone Rebar Ownership health can no longer return clean solely because a null semantic element was skipped, focused regression coverage pins direct fail-closed behavior and aggregate compatibility, and this claim is closed after merged-main readback.
