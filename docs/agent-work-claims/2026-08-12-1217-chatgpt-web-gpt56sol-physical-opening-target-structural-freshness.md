# Work claim — Physical Opening target structural freshness

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-physical-opening-target-structural-freshness-20260812-1217`
- Registered: `2026-08-12T12:17:00+07:00`
- Completed: `2026-08-12T12:21:00+07:00`
- Baseline main SHA: `595309c39e1d1a7cd47c8bb6043ca2245d24bbf2`
- Claim commit: `bd8943db339a217f003b9ebb2c5fcc1b59a2edab`
- Source fix commit: `ccebcae6b813c9bca254e64cccad6410331cbf72`
- Focused smoke commit: `592602dd3a9c3d9a99d83d0614548edf0e62924f`
- Integration PR: `#873`
- Main integration SHA: `b9833b81f1c7caebeaa4a75a574c434842547368`
- Priority: P1 — physical cut target resolution must not silently switch to replacement opening instances under an unchanged ChangeVersion.
- Task Key: `CORE-PHYSICAL-OPENING-TARGET-STRUCTURAL-FRESHNESS`

## Confirmed defect

`PhysicalOpeningCutTargetStateCodec.Resolve(...)` validated project IDs and the exact host instance before caller target-ID enumeration and pinned `ProjectState.ChangeVersion`, but it did not pin target opening instances. A lazy target source could directly replace a Door/WallOpening with a same-ID instance through public `project.Elements` without calling `Touch()`. Post-enumeration ID validation still passed and the resolver could return the replacement opening under the unchanged revision.

## Implemented contract

- Resolution snapshots exact ordered project element references plus a case-insensitive ID -> instance index before lazy target-ID enumeration.
- Existing `ChangeVersion` freshness rejection and target normalization/canonicalization remain intact.
- Count/order/reference structural drift is rejected after target enumeration before physical opening resolution.
- Target openings are resolved from the captured ownership snapshot rather than a changed live list.
- Existing Door/WallOpening category and canonical `HostWallId` relation checks remain unchanged.
- Project revision and structure are checked again before returning resolved targets.
- Host ownership, target-state Base64/UTF-8 serialization rules, count/length bounds and result ordering are unchanged.

## Regression evidence

`tests/QS3D.Core.SmokeTests/PhysicalOpeningCutTargetStructuralFreshnessSmoke.cs` creates a `W1` host and project-owned Door `D1`. A lazy target source replaces `D1` with a new same-ID Door linked to the same host without calling `Touch()`; resolution must reject structural drift while `ChangeVersion` remains unchanged. A stable control requires the exact project-owned opening instance to be returned.

## Integration / concurrency evidence

The branch diff from claim commit contained exactly the reserved codec source plus focused smoke. Four commits between claim `bd8943db...` and reviewed moving `main@5b7a9a79...` did not touch either reserved file. Current-main readback immediately before merge still had exact pre-fix codec blob `1fecf485c93380e8ec71185ea597f0b9789ac663`. PR #873 was squash-merged with expected head `592602dd3a9c3d9a99d83d0614548edf0e62924f` as `b9833b81f1c7caebeaa4a75a574c434842547368`.

## Validation boundary

No GitHub Actions were dispatched. No force-push was used. No local .NET/full executable smoke or licensed BricsCAD V25/V26 runtime PASS is claimed from this connector-only lane.
