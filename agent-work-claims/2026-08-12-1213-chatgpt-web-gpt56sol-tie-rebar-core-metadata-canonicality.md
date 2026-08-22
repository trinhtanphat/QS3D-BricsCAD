# Work claim — Tie Rebar core generated metadata canonicality

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-tie-rebar-core-metadata-canonicality`
- Registered: `2026-08-12T12:13:00+07:00`
- Completed: `2026-08-12T12:18:00+07:00`
- Baseline main SHA: `6d93e196d07b02afc59d71aa42f83ac283a7a706`
- Priority: P1 — generated Tie Rebar count/diameter/actual-spacing metadata must preserve writer-owned serialization.
- Task Key: `CORE-TIE-REBAR-CORE-METADATA-CANONICALITY`

## Confirmed defect

`ColumnTieSolidBuilder.CommitSemanticUpdate(...)` persists `GeneratedTieRebarCount` with invariant `int.ToString`, `GeneratedTieRebarDiameterMm` with `double.ToString("R", CultureInfo.InvariantCulture)`, and `GeneratedTieRebarActualSpacingM` with `double.ToString("R", CultureInfo.InvariantCulture)`.

`GeneratedTieRebarHealthService` accepted count through integer parsing/count equality and diameter/actual-spacing through numeric domain checks only. Alternate raw spellings such as `01`, `10.0`, or `0.200` could therefore pass health even though the writer never emits those spellings.

## Completed implementation

- Claim commit: `f9a0c45adc9d03b9ea4739c4e67756f9448e1726`.
- Source commit: `45232d4ac730cee98884239bead2fc0914ff8782`.
- Smoke commit: `866d974703fbc5bb8f33af189919ed89d7918a8c`.
- PR #867 squash merge: `8868ada7b5054b5af8cd7a585d5fe5e7edff9d49`.
- Merged source blob read back from `main`: `9ae5ee63c2ca067a6d56fc77cf782abdab7c7f14`.
- Merged smoke blob read back from `main`: `e6d67adc0fc8396dd848c14e0e152382ddfada12`.
- Ancestry verified: merge `8868ada7b5054b5af8cd7a585d5fe5e7edff9d49` is an ancestor of `main@6d3dd8a4e7562fc9d0bedc93207b55e88d98a675`; subsequent commits in that compare did not touch the Tie Rebar provider or smoke.

## Final contract

- A count that parses and equals the valid handle count must use exact invariant integer spelling or emits `TIE_REBAR_GENERATED_COUNT_NON_CANONICAL` as Error.
- A finite positive diameter must use exact round-trip invariant spelling or emits `TIE_REBAR_GENERATED_DIAMETER_NON_CANONICAL` as Error.
- A finite nonnegative actual spacing must use exact round-trip invariant spelling or emits `TIE_REBAR_GENERATED_SPACING_NON_CANONICAL` as Error.
- Existing mismatch/invalid precedence remains unchanged; invalid values do not receive canonicality noise.
- Exact writer-owned values, including zero actual spacing, preserve existing behavior.

No GitHub Actions were dispatched. No full local .NET build PASS, executable smoke PASS, or BricsCAD V25/V26 runtime PASS is claimed for this lane.
