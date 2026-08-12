# Work claim — Generated Tie Rebar health null-element fail-visible

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-tie-rebar-null-health`
- Registered: `2026-08-12T07:43:00+07:00`
- Baseline main SHA: `3b5245820b5d346a4b8fdfbfac30ba97cd9d844e`
- Priority: P1 — standalone generated health must not silently treat malformed ProjectState entries as clean.
- Task Key: `CORE-TIE-REBAR-NULL-HEALTH`

## Confirmed defect

`GeneratedTieRebarHealthService.Inspect(ProjectState, ...)` and its internal ownership-index traversal execute `if (element == null) continue;`. A malformed project containing a null semantic element can therefore be silently normalized inside the standalone provider. Newer generated-health lanes use a fail-visible contract: direct inspection rejects malformed null entries with `InvalidOperationException`, while `ComprehensiveModelHealthService.AddSafely(...)` converts that bounded failure into a stable Error-level `HEALTH_PROVIDER_FAILED` issue.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedTieRebarHealthService.cs`
- one focused auto-discovered `scripts/preflight-*.py` regression gate
- this claim file

Do not modify tie-rebar builders, rebar notation/fabrication, quantity semantics, ownership policy/index, CAD runtime code, or `ComprehensiveModelHealthService`.

## Intended contract

- Direct Tie Rebar health inspection and its ownership traversal throw `InvalidOperationException` on a null project element instead of silently skipping it.
- Valid projects retain all existing handle/count/diameter/spacing/category/stale/ownership diagnostics.
- Composite health reuses existing `AddSafely` handling and remains fail-visible via `HEALTH_PROVIDER_FAILED` without aggregate changes.
- Inspection remains read-only.
- No GitHub Actions/build/release dispatch and no executable Core/full-build/BricsCAD runtime PASS claim from this remote lane.

## Completion condition

Standalone Tie Rebar health can no longer return clean solely because a null semantic element was skipped, focused regression coverage pins direct fail-closed behavior and aggregate compatibility, and this claim is closed after merged-main readback.
