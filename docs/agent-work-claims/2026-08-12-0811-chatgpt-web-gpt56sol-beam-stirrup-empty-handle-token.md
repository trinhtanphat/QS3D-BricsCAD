# Work claim — Generated Beam Stirrup empty handle token fail-closed

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-beam-stirrup-empty-handle-token`
- Registered: `2026-08-12T08:11:27+07:00`
- Baseline main SHA: `afd42195e80aad45f54a0630f110154a6e9f436d`
- Priority: P1 — malformed generated-handle metadata must not be silently normalized by health diagnostics.
- Task Key: `CORE-BEAM-STIRRUP-EMPTY-HANDLE-TOKEN`

## Confirmed defect

`GeneratedBeamStirrupHealthService.Inspect(ProjectState, ...)` explicitly treats `handle.Length == 0` as `INVALID_BEAM_STIRRUP_GENERATED_HANDLE`, but the validation loop first calls `raw.Split(..., StringSplitOptions.RemoveEmptyEntries)`. Empty semicolon tokens are therefore removed before validation, so metadata such as `AA;;BB`, `;AA`, or `AA;` can bypass the invalid-handle branch when the persisted count matches the surviving valid handles.

## Non-overlap check

The Beam Stirrup null-health lane is already completed. Recent single-bind/layout lanes are separate from this Core diagnostics parser. No recent empty-handle-token claim/commit was found for Beam Stirrup before registration.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedBeamStirrupHealthService.cs`
- one focused `scripts/preflight-*.py` regression gate
- this claim file

Do not modify stirrup builders, layout/geometry/advanced metadata semantics, generated ownership policy, CAD runtime code, or unrelated diagnostics.

## Intended contract

- Preserve empty delimiter tokens while validating `GeneratedBeamStirrupHandles`.
- Empty or whitespace-only tokens emit `INVALID_BEAM_STIRRUP_GENERATED_HANDLE` instead of being silently removed.
- Valid canonical handle lists retain existing duplicate, ownership, live-solid, count, advanced metadata, category, and stale behavior.
- Ownership indexing remains unchanged; inspection remains read-only.
- No GitHub Actions/build/release dispatch and no BricsCAD V25 runtime PASS claim from this remote lane.

## Completion condition

Malformed leading/trailing/repeated-delimiter Beam Stirrup handle metadata is fail-visible, a focused static regression gate protects the validation loop, source + gate are read back from merged `main`, and this claim is closed with exact commit SHAs.
