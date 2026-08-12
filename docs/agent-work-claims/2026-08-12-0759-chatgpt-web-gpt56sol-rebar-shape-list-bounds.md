# Work claim — Rebar shape list finite bounds

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-rebar-shape-list-bounds-20260812-0759`
- Registered: `2026-08-12T07:59:00+07:00`
- Completed: `2026-08-12T08:01:00+07:00`
- Baseline main SHA: `c5b51651bed94fbb54268b506c338c8547f4c966`
- Claim commit: `ab9c57dc18f6bf1d80871ca748b7aa2094e2d609`
- Source commits: `e5441f2f2945ed77b343c3cb6a5365785b73a520`, `c8181892b29fb3eb394fb008172747f5b337428c`
- Regression commit: `17fe277b5d280978dd792dced0f58d5e89f95c09`
- Pre-close verification main SHA: `b7fe7b7d59f510cd19fca85dd4f2dbfaf0bd9372`
- Priority: evidence-driven remote-safe resource bounds during owner-requested `continue all`

## Confirmed defect

`RebarShapePathBuilder` advertised `MaxLegs = 32`, but `ParsePositiveList()` and `ParseTurns()` called `string.Split(...)` and materialized every token before the leg-count contract was enforced. Persisted `RebarShapeLegsM` / `RebarShapeTurnsDeg` strings therefore had no pre-allocation bound and could consume memory/CPU far beyond the supported 32-leg shape before eventually failing.

## Completed change

- Legs and turns text longer than 4096 UTF-16 characters now fail before `Split()`.
- Leg parsing fails as soon as token 33 is reached instead of materializing an unsupported shape list first.
- Explicit turn parsing is capped to the current legal `legs - 1` cardinality while the existing exact-cardinality check remains in place.
- Nullable flow is made explicit with a non-null local before length/split access so the strict warnings-as-errors gate is not asked to infer repeated nullable property state.
- Accepted shape codes, invariant numeric grammar, finite/positive leg semantics, ±180° turn policy, cutting-length validation and generated geometry are unchanged.

## Regression coverage

`RebarShapePathBoundsSmoke` pins:

- exactly 4096 characters accepted and 4097 rejected;
- exactly 32 custom legs accepted and 33 rejected;
- excess explicit turns rejected;
- ordinary straight, L and U presets unchanged.

## Scope respected

No CAD/native placement, Level Z-chain, shape distribution, fabrication standards, generated ownership, BBS quantity semantics, release/update or UI changes were made. The completed RebarShapePath collection-aliasing contract was not modified.

## Validation evidence

Source and smoke were re-fetched from current `main@b7fe7b7d59f510cd19fca85dd4f2dbfaf0bd9372` after concurrent commits and both changes remained present. This web session performed source/static read-back only: no GitHub Actions were dispatched, no local `dotnet`/Core smoke execution is claimed, and no BricsCAD V25/V26 runtime qualification is claimed.
