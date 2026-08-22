# Work claim — Door/opening schedule signed-zero grouping canonicality

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T10:43:00+07:00`
- Completed: `2026-08-12T10:47:00+07:00`
- Baseline main SHA: `df0a2626a3fb99bd70af57e75660a3fd0a0f496e`
- Priority: evidence-driven remote-safe reporting grouping integrity

## Reason

`DoorOpeningScheduleBuilder` accepts zero as a valid sill height and thickness, then serialized those values with round-trip (`"R"`) formatting inside the schedule grouping key. IEEE-754 positive and negative zero are physically equivalent for these non-negative dimensions but can retain distinct textual representations, allowing otherwise-identical door/opening rows to split solely because a zero value carries the negative-zero sign bit.

## Changed scope

Zero-valued numeric grouping tokens are now canonicalized to positive zero before round-trip formatting. Non-zero dimensions, length-prefixed group-key framing, category/family/material identity, host handling, quantity aggregation and output ordering remain unchanged.

## Changed surfaces

- `src/QS3D.Core/Reporting/DoorOpeningSchedule.cs`
- `tests/QS3D.Core.SmokeTests/DoorOpeningScheduleSignedZeroSmoke.cs`
- this claim file

## Completion

- Claim commit: `0f0cd0a58e8d22b75f8b7fc51cfe24aec8da1202`.
- Implementation commit: `4bbbeb52344131d6acb6d1edd99a00fd585bde44` — route all schedule grouping dimension tokens through signed-zero canonical round-trip formatting.
- Regression commit: `3794dae06b566c6e0e88e165d051b6da73e3c2a2` — prove `0` and `-0` sill/thickness inputs coalesce into one row while a real non-zero sill remains a distinct row.
- Validation actually performed:
  - fetched the implementation diff and confirmed only four group-key call sites plus `CanonicalNumber` changed;
  - re-fetched current builder source and confirmed signed-zero canonicalization remains present;
  - re-fetched the dedicated smoke source and checked signed-zero equivalence plus non-zero sensitivity coverage;
  - no repository `dotnet` tests were executed in this hosted session;
  - no GitHub Actions were dispatched or rerun;
  - no BricsCAD V25/V26 runtime PASS is claimed.

## Coordination

The concurrent door/opening XLSX row-snapshot claim concerns export-time row materialization and is disjoint from this non-IO schedule grouping key. No signed-zero grouping claim was present before this scope was reserved.

## Completion condition

Satisfied: current `main` no longer splits otherwise-identical door/opening schedule rows solely because a zero-valued grouping dimension carries the IEEE-754 negative-zero sign bit, focused regression coverage is present, and this claim is released as `COMPLETED`.
