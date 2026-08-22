# Work claim — Beam Stirrup actual-spacing health integrity

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-beam-stirrup-actual-spacing-health`
- Registered: `2026-08-12T11:39:00+07:00`
- Completed: `2026-08-12T11:50:00+07:00`
- Baseline main SHA: `72d81943726687163a9e0fb7e765ebd1e00c81af`
- Priority: P1 — writer-owned generated Beam Stirrup spacing metadata must not bypass health validation.
- Task Key: `CORE-BEAM-STIRRUP-ACTUAL-SPACING-HEALTH`

## Confirmed defect

`BeamStirrupSolidBuilder` always persists `GeneratedBeamStirrupActualSpacingM` from `BeamStirrupLayoutPlanner.ActualSpacingM` using `double.ToString("R", CultureInfo.InvariantCulture)`. `GeneratedBeamStirrupHealthService` did not read this persisted field. A generated stirrup could therefore carry malformed `NaN`, `Infinity`, negative spacing, or an alternate non-writer spelling without any spacing-specific health evidence.

`BeamStirrupLayoutPlanner` permits a single-stirrup layout, so zero actual spacing is valid; the domain bound is finite and nonnegative, not strictly positive.

## Completed implementation

- Claim commit: `d2df95c7d936c74ab1b29c598407fd547fe6ec0b`.
- Reviewed source commit on original branch: `2875c8ffb43d8c0756ee5c9ce79a70e5e5c8ab60`.
- Reviewed smoke commit on original branch: `c55c16099c3a9971f9f3cdecfb7e919f4ded0fc8`.
- PR #834 merged concurrently while a fresh replacement was being prepared; authoritative merge commit: `86ed1ecf7ce2189f9ba64b35354dea6f0fb695b4`.
- Replacement PR #840 was closed unmerged after detecting #834 had already merged; no duplicate code was merged.
- Merged source blob read back from `main`: `895c52f150d2de817edc2114c014b934d2e90ea7`.
- Merged smoke blob read back from `main`: `9aed8f132bb8dce9d440b2acb48cbf9351e49c3c`.
- Ancestry verified: merge `86ed1ecf7ce2189f9ba64b35354dea6f0fb695b4` is an ancestor of `main@7c160de66de68c811282f4cd460e927370e454cd`; subsequent commits in that compare did not touch the source or smoke.

## Final contract

- Present non-empty actual-spacing metadata must parse as an invariant finite number >= 0 or emits `BEAM_STIRRUP_ACTUAL_SPACING_INVALID`.
- After numeric/domain validity, raw text must equal `value.ToString("R", CultureInfo.InvariantCulture)` or emits `BEAM_STIRRUP_ACTUAL_SPACING_NON_CANONICAL`.
- Invalid values do not receive canonicality noise.
- Exact writer-owned `0` and positive round-trip values preserve existing behavior.
- Missing legacy actual-spacing metadata remains compatible.
- Inspection remains read-only and deterministic.

No GitHub Actions were dispatched. No full local .NET build PASS and no BricsCAD V25/V26 runtime PASS are claimed for this lane.
