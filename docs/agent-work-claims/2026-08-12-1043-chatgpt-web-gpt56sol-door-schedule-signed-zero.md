# Work claim — Door/opening schedule signed-zero grouping canonicality

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T10:43:00+07:00`
- Baseline main SHA: `df0a2626a3fb99bd70af57e75660a3fd0a0f496e`
- Priority: evidence-driven remote-safe reporting grouping integrity

## Reason

`DoorOpeningScheduleBuilder` accepts zero as a valid sill height and thickness, then serializes those values with round-trip (`"R"`) formatting inside the schedule grouping key. IEEE-754 positive and negative zero are physically equivalent for these non-negative dimensions but can retain distinct textual representations, allowing otherwise-identical door/opening rows to split solely because a zero value carries the negative-zero sign bit.

## Intended scope

Canonicalize zero-valued numeric grouping tokens to positive zero before round-trip formatting while preserving all non-zero dimensions, length-prefixed group-key framing, category/family/material identity, host handling, quantity aggregation and output ordering.

## Changed surfaces

- `src/QS3D.Core/Reporting/DoorOpeningSchedule.cs`
- focused smoke under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Validation boundary

Remote/static validation only in this hosted session. Do not dispatch/rerun GitHub Actions and do not claim BricsCAD V25/V26 or local .NET runtime PASS without actual supported runtime execution.
