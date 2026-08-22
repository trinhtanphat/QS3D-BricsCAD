# Work claim — Generated Rebar count/diameter canonicality

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-generated-rebar-count-diameter-canonicality`
- Registered: `2026-08-12T12:20:00+07:00`
- Completed: `2026-08-12T12:24:00+07:00`
- Baseline main SHA: `e7f78bed2809247057ce790b4c2d1c3133b0603b`
- Priority: P1 — writer-owned generated longitudinal/shape rebar core metadata must preserve exact serialization.
- Task Key: `CORE-GENERATED-REBAR-COUNT-DIAMETER-CANONICALITY`

## Confirmed defect

`ColumnRebarSolidBuilder` and `BeamRebarSolidBuilder` both persist `GeneratedRebarCount` with invariant `int.ToString(...)` and `GeneratedRebarDiameterMm` with `double.ToString("R", CultureInfo.InvariantCulture)`. `ShapeRebarSolidBuilder` persists `GeneratedShapeRebarCount` with invariant `int.ToString(...)`.

`GeneratedRebarHealthService` previously validated longitudinal/shape counts through integer parsing plus expected-count equality and validated longitudinal diameter through finite positive numeric parsing only. Alternate raw spellings such as `01` or `10.0` could therefore pass health even though the writers never emit them.

## Completed implementation

- Claim commit: `5b7a9a799b77ec70e80d84052d7137931cba911f`.
- Source commit: `0ba4d1aeca35aca9e6ea5073fa273a620f858dd8`.
- Smoke commit: `4e8c9089a0157668977fa555d7ab9a935a179d68`.
- PR #875 squash merge: `9e205167228c171c058c9d74d2394828f433435c`.
- Merged source blob read back from `main`: `093c9debb8f8d895378467f6ca19c6ba71aec686`.
- Merged smoke blob read back from `main`: `7cbd7ecb5e1e5983f94c14a88d2c4f4d2b600768`.
- `main` readback immediately after merge was `9e205167228c171c058c9d74d2394828f433435c`, so the merge is the current verified ancestor/root of the snapshot.

## Final contract

- A longitudinal count that parses and equals the valid generated-handle count must use exact invariant integer spelling or emits `REBAR_GENERATED_COUNT_NON_CANONICAL` as Error.
- A shape count that parses and equals the valid generated-shape handle count must use exact invariant integer spelling or emits `SHAPE_REBAR_GENERATED_COUNT_NON_CANONICAL` as Error.
- A finite positive longitudinal generated diameter must use exact round-trip invariant spelling or emits `REBAR_GENERATED_DIAMETER_NON_CANONICAL` as Error.
- Existing count mismatch/missing-count and diameter invalid precedence remains unchanged; invalid/mismatched values do not receive canonicality noise.
- Exact writer-owned values preserve existing behavior.
- Generated rebar mode semantics remain out of scope because that lane already exists in repository history.

No GitHub Actions were dispatched. No full local .NET build PASS, executable smoke PASS, or BricsCAD V25/V26 runtime PASS is claimed for this lane.
