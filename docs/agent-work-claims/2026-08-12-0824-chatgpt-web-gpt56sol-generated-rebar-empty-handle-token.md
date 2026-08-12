# Work claim — Generated Rebar empty handle token fail-closed

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-generated-rebar-empty-handle-token`
- Registered: `2026-08-12T08:24:00+07:00`
- Baseline main SHA: `ff0422adbc9814e730cc60c293785053b11749b5`
- Priority: P1 — malformed generated rebar handle metadata must not be silently normalized by health diagnostics.
- Task Key: `CORE-GENERATED-REBAR-EMPTY-HANDLE-TOKEN`

## Confirmed defect

`GeneratedRebarHealthService.InspectSet(...)` parses both `GeneratedRebarHandles` and `GeneratedShapeRebarHandles` with `StringSplitOptions.RemoveEmptyEntries`, then immediately contains a `handle.Length == 0` invalid-handle branch. Empty semicolon tokens are therefore removed before validation, so malformed metadata such as `AA;;BB`, `;AA`, or `AA;` can bypass `INVALID_REBAR_GENERATED_HANDLE` / `INVALID_SHAPE_REBAR_GENERATED_HANDLE` when the persisted count matches the surviving valid handles.

## Non-overlap check

The Tie Rebar empty-handle lane is already completed and touches a different provider. Recent Shape Rebar work concerns distribution/list bounds and builder/runtime behavior, not this Core generated-health parser. No recent claim/commit was found for generic `GeneratedRebarHealthService` empty handle tokens before registration.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedRebarHealthService.cs`
- one focused `scripts/preflight-*.py` regression gate for both handle-set specs
- this claim file

Do not modify rebar builders, distribution/shape parsing, ownership policy, CAD runtime code, or unrelated diagnostics.

## Intended contract

- Preserve empty delimiter tokens in `InspectSet(...)` validation for both column/beam longitudinal and shape generated rebar handle sets.
- Empty or whitespace-only tokens emit the existing `INVALID_<PREFIX>_GENERATED_HANDLE` diagnostics instead of being silently removed.
- Valid canonical handle lists retain existing duplicate, ownership, liveness, count and diameter behavior.
- Ownership indexing remains unchanged; inspection remains read-only.
- No GitHub Actions/build/release dispatch and no BricsCAD V25 runtime PASS claim from this remote lane.

## Completion condition

Malformed leading/trailing/repeated-delimiter generated rebar handle metadata is fail-visible for both specs, a focused static regression gate protects the validation loop, source + gate are read back from merged `main`, and this claim is closed with exact commit SHAs.
