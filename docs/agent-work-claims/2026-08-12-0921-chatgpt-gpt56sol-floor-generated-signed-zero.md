# Work claim — Floor generated identity signed-zero canonicality

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-floor-generated-signed-zero`
- Registered: `2026-08-12T09:21:00+07:00`
- Last Updated: `2026-08-12T09:21:00+07:00`
- Baseline main SHA: `64fa8482fbfe498dbbce2780638bd9e95ec5e7fc`
- Priority: deterministic generated-state canonicality defect found during owner-requested continue-all audit
- Task Key: `CORE-FLOOR-GENERATED-SIGNED-ZERO`

## Confirmed defect

`FloorGeneratedIdentityPlanner.Create(...)` writes finite `ElevationM` directly with `ToString("R", InvariantCulture)` into the generated state key. The project Floor mutation contract treats `-0.0` and `0.0` as the same elevation (`ProjectFloorService.NearlyEqual`), while other generated fingerprints now explicitly canonicalize signed zero before round-trip formatting. A sign-only zero representation can therefore produce a different Floor state token for semantically unchanged elevation and trigger avoidable generated-state churn.

## Reserved scope

- `src/QS3D.Core/Domain/FloorGeneratedIdentityPlanner.cs`
- `tests/QS3D.Core.SmokeTests/FloorGeneratedIdentitySmoke.cs`
- this claim file

## Intended contract

Canonicalize only signed zero before invariant round-trip elevation formatting. Preserve all non-zero finite values, owner identity semantics, Unicode/length validation, token prefixes and hashing format.

## Validation plan

Extend the existing registered Floor generated identity smoke to prove `+0.0` and `-0.0` produce identical `StateKey`/`StateToken` and canonical `ElevationM`, while a real non-zero elevation change still changes state identity. Re-fetch exact source/claim before writes. No GitHub Actions/build/runtime PASS claimed unless actually executed.

## Completion condition

Semantically identical positive/negative zero elevations generate identical Floor state identity without changing non-zero behavior.
