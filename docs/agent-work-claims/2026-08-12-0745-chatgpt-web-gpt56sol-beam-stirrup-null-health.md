# Work claim — Generated Beam Stirrup health null-element fail-visible

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-beam-stirrup-null-health`
- Registered: `2026-08-12T07:45:00+07:00`
- Baseline main SHA: `8e24829a0eed1938bc8537043a1ec248db0089ca`
- Priority: P1 — standalone generated health must not silently treat malformed ProjectState entries as clean.
- Task Key: `CORE-BEAM-STIRRUP-NULL-HEALTH`

## Confirmed defect

`GeneratedBeamStirrupHealthService.Inspect(ProjectState, ...)` and its internal ownership traversal silently skip null semantic elements. A malformed project containing a null semantic element can therefore be normalized away inside this standalone provider. Newer generated-health lanes use a fail-visible contract: direct inspection rejects malformed null entries with `InvalidOperationException`, while `ComprehensiveModelHealthService.AddSafely(...)` converts the bounded failure into a stable Error-level `HEALTH_PROVIDER_FAILED` issue.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedBeamStirrupHealthService.cs`
- one focused auto-discovered `scripts/preflight-*.py` regression gate
- this claim file

Do not modify stirrup builders, rebar notation/fabrication, quantity semantics, ownership policy/index, CAD runtime code, or `ComprehensiveModelHealthService`.

## Intended contract

- Direct Beam Stirrup health inspection and any internal ownership traversal throw `InvalidOperationException` on a null project element instead of silently skipping it.
- Valid projects retain existing handle/count/diameter/spacing/category/stale/ownership diagnostics.
- Composite health reuses existing `AddSafely` handling and remains fail-visible via `HEALTH_PROVIDER_FAILED` without aggregate changes.
- Inspection remains read-only.
- No GitHub Actions/build/release dispatch and no executable Core/full-build/BricsCAD runtime PASS claim from this remote lane.

## Completion condition

Standalone Beam Stirrup health can no longer return clean solely because a null semantic element was skipped, focused regression coverage pins direct fail-closed behavior and aggregate compatibility, and this claim is closed after merged-main readback.
