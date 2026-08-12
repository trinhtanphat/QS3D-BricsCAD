# Work claim — Element relation canonicality in Model Health

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-element-relation-canonicality`
- Registered: `2026-08-12T08:47:00+07:00`
- Baseline main SHA: `924a861697e4f0b54660d0449f334922dccd4393`
- Priority: P1 — malformed persisted Family/Floor/Zone element relations must not be silently normalized by baseline Model Health.
- Task Key: `CORE-MODEL-HEALTH-ELEMENT-RELATION-CANONICALITY`

## Confirmed defect

`ProjectFamilyService.Assign`, `ProjectFloorService.Assign` and `ProjectZoneService.Assign` persist exact canonical target IDs. `ModelHealthService.ValidateFamily`, `ValidateFloor` and `ValidateZone`, however, trim the public-settable `ProjectElement.FamilyId`, `FloorId` and `ZoneId` before validation. Directly mutated or malformed persisted relation text such as `" F1 "`, `" L1 "` or `" Z1 "` can therefore resolve to a valid target without health evidence even though the writer never emits that spelling.

## Non-overlap check

The recent Model Health baseline-input-integrity claim is completed. The Level Reference canonicality lane only covers `BottomLevelId`/`TopLevelId`, not `ProjectElement.FloorId`. Dependency and Host relations are explicitly excluded. No recent claim/commit was found for canonical spelling of these three baseline element relation fields.

## Reserved scope

- `src/QS3D.Core/Diagnostics/ModelHealthService.cs`
- one focused Core smoke regression for Family/Floor/Zone relation spelling
- this claim file

Do not modify ProjectElement setters, Family/Floor/Zone mutation services, HostWallId, DependsOn, identity collections, persistence/interchange or BricsCAD runtime code.

## Intended contract

- Padded non-empty FamilyId/FloorId/ZoneId emit dedicated `HealthSeverity.Error` canonicality diagnostics while retaining existing missing/ambiguous/category checks on the normalized target.
- Whitespace-only stored relation values also fail visible as non-canonical in addition to existing missing relation evidence.
- Canonical IDs preserve current behavior.
- Inspection remains read-only and deterministic.

## Completion condition

Malformed Family/Floor/Zone relation spellings are fail-visible, focused Core smoke coverage pins padded cases plus canonical controls, source + smoke are read back from merged `main`, ancestry is verified, and this claim is closed with exact commit SHAs.
