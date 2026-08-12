# Work claim — Element relation canonicality in Model Health

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-element-relation-canonicality`
- Registered: `2026-08-12T08:47:00+07:00`
- Baseline main SHA: `924a861697e4f0b54660d0449f334922dccd4393`
- Priority: P1 — malformed persisted Family/Floor/Zone element relations must not be silently normalized by baseline Model Health.
- Task Key: `CORE-MODEL-HEALTH-ELEMENT-RELATION-CANONICALITY`

## Confirmed defect

`ProjectFamilyService.Assign`, `ProjectFloorService.Assign` and `ProjectZoneService.Assign` persist exact canonical target IDs. `ModelHealthService.ValidateFamily`, `ValidateFloor` and `ValidateZone` previously trimmed the public-settable `ProjectElement.FamilyId`, `FloorId` and `ZoneId` before validation. Directly mutated or malformed persisted relation text such as `" F1 "`, `" L1 "` or `" Z1 "` could therefore resolve to a valid target without health evidence even though the writer never emits that spelling.

## Implemented fix

- `ValidateFamily` now retains raw `FamilyId` long enough to report `FAMILY_REFERENCE_NON_CANONICAL` with `HealthSeverity.Error` when trimming changes the stored spelling.
- `ValidateFloor` does the same through `FLOOR_REFERENCE_NON_CANONICAL`.
- `ValidateZone` does the same through `ZONE_REFERENCE_NON_CANONICAL`.
- Existing normalized lookup is preserved after canonicality evidence so missing/ambiguous/category behavior remains unchanged.
- Whitespace-only relation text now produces canonicality evidence in addition to the existing missing relation issue.
- HostWallId, DependsOn, identity collection logic and mutation services are unchanged.

## Regression coverage

`tests/QS3D.Core.SmokeTests/ModelHealthElementRelationCanonicalitySmoke.cs` covers:

- padded FamilyId;
- padded FloorId;
- padded ZoneId;
- whitespace-only FamilyId;
- canonical Family/Floor/Zone controls.

The fixture uses a Grid element with canonical Family/Floor/Zone objects to avoid unrelated dimension/material diagnostics.

## Integration evidence

- Claim registration: `bfd4611b6fde851c2c2cd466407088f749426011`.
- Source fix: `11afaeaffa18872a9a92ff376070d640ce6bf2f0`.
- Focused Core smoke: `00e9673cf0383b5c8e6361afe5980e34da2ede08`.
- Source and smoke were read back from current `main` after concurrent commits.
- Comparison from smoke commit `00e9673cf0383b5c8e6361afe5980e34da2ede08` to then-current `main` `6bc5c2650da19e3dd7d60853e486bf6a56ab6335` was `ahead`, `ahead_by=1`, `behind_by=0`, with the smoke commit as merge base; the intervening commit only updated an unrelated browser Element ID claim.

## Validation boundary

Committed deterministic Core smoke coverage plus source/readback/ancestry review. No GitHub Actions were dispatched, no full local .NET build PASS is claimed, and no licensed BricsCAD V25 runtime PASS is claimed.
