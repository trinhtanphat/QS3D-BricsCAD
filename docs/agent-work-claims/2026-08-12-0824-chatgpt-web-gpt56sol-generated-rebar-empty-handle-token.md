# Work claim — Generated Rebar empty handle token fail-closed

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-generated-rebar-empty-handle-token`
- Registered: `2026-08-12T08:24:00+07:00`
- Baseline main SHA: `ff0422adbc9814e730cc60c293785053b11749b5`
- Priority: P1 — malformed generated rebar handle metadata must not be silently normalized by health diagnostics.
- Task Key: `CORE-GENERATED-REBAR-EMPTY-HANDLE-TOKEN`

## Confirmed defect

`GeneratedRebarHealthService.InspectSet(...)` parsed both `GeneratedRebarHandles` and `GeneratedShapeRebarHandles` with `StringSplitOptions.RemoveEmptyEntries`, then immediately contained a `handle.Length == 0` invalid-handle branch. Empty semicolon tokens were therefore removed before validation, so malformed metadata such as `AA;;BB`, `;AA`, or `AA;` could bypass `INVALID_REBAR_GENERATED_HANDLE` / `INVALID_SHAPE_REBAR_GENERATED_HANDLE` when the persisted count matched the surviving valid handles.

## Non-overlap check

The Tie Rebar empty-handle lane was already completed and touches a different provider. Recent Shape Rebar work concerns distribution/list bounds and builder/runtime behavior, not this Core generated-health parser. No prior generic `GeneratedRebarHealthService` empty-handle-token claim/commit was found before registration.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedRebarHealthService.cs`
- `scripts/preflight-generated-rebar-empty-handle-token.py`
- this claim file

No rebar builders, distribution/shape parsing, ownership policy, CAD runtime code, or unrelated diagnostics were modified.

## Implemented contract

- `InspectSet(...)` now preserves empty delimiter tokens with `StringSplitOptions.None` for both column/beam longitudinal and shape generated rebar handle sets.
- Empty or whitespace-only tokens reach the existing `INVALID_<PREFIX>_GENERATED_HANDLE` diagnostic instead of being silently removed.
- Valid canonical handle lists retain existing duplicate, ownership, liveness, count and diameter behavior.
- Ownership indexing remains unchanged and still uses its canonical `SplitHandles(...)` normalization path; inspection remains read-only.

## Integration evidence

- Claim registration: `e1e9b393859784ae0c1cb461b257df4c0b73a0ca`.
- Source fix: `5b7e41054c1dfd570a2f21b5d1d282182133a7b2`.
- Regression/preflight gate: `80278015045be633c855b4ed9f0e84887c049242`.
- Readback on `main` at `bced52bb8d8ec74571415b0b62fedbff81ce38f8` confirmed `InspectSet(...)` uses `StringSplitOptions.None` and the invalid-handle branch remains present.
- `80278015045be633c855b4ed9f0e84887c049242` was verified as an ancestor of the readback `main` (`behind_by = 0`).

## Validation boundary

Static source/gate readback and ancestry were verified remotely. No GitHub Actions/build/release dispatch was performed and no local .NET or licensed BricsCAD V25 runtime PASS is claimed.
