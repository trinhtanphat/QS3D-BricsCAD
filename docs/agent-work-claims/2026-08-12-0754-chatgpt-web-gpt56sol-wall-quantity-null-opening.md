# Work claim — Wall quantity null opening guard

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12`
- Baseline main SHA: `0151f4b9ff18e9956c4b3d25530cdc0d1bd4c06a`
- Priority: quantity correctness / malformed-input fail-closed behavior

## Confirmed defect

`WallQuantityCalculator.Calculate(...)` accepted an optional enumerable of opening cuts, but silently skipped enumerated `null` entries. A malformed opening collection could therefore understate opening area and deduction volume while still returning apparently valid wall quantities.

## Completed contract

1. `openings == null` keeps the existing no-opening behavior.
2. A non-null enumerable containing a null entry now fails closed with `ArgumentException`.
3. Valid opening calculations remain unchanged, including clamping total opening area to gross wall area.
4. No semantic regenerator, native BricsCAD, host-link, reporting, or export behavior was changed.

## Commits

- Claim registration: `ac48b719a339968ae97ead369c2bbb25d6f2816a`
- Planning: `117f529eaf88b8b30ddc8a788e849924915f0eb6`
- Source fix: `547b759a4ae6d6808e6194ace1f5c96d8d893b2f`
- Focused smoke regression source: `86aacc11e2229d8b70e1d1b85b564a17a5be44ae`

## Validation evidence

- A first smoke-file write hit a GitHub `409` because `main` advanced concurrently; no false commit was reported. HEAD and source ancestry were refreshed before retrying the write.
- Source and smoke commits were verified as ancestors of observed `main` `3a766aeb9192ae12d42fc4f9bd2d27b05baaae37` with `behind_by: 0`.
- Concurrent commits after the source change did not modify `WallQuantityCalculator.cs`.
- Smoke source covers null collection, null entry rejection, normal valid opening calculation, and oversized opening clamping.
- Regression source was committed but GitHub Actions were not dispatched in this remote session.
- No CI PASS, build PASS, licensed BricsCAD runtime PASS, or release publication is claimed.

## Released scope

This claim is complete; `WallQuantityCalculator.cs` is released for other agents.
