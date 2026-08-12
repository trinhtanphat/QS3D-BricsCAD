# Work claim — Physical Opening target structural freshness

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-physical-opening-target-structural-freshness-20260812-1217`
- Registered: `2026-08-12T12:17:00+07:00`
- Baseline main SHA: `595309c39e1d1a7cd47c8bb6043ca2245d24bbf2`
- Priority: P1 — physical cut target resolution must not silently switch to replacement opening instances under an unchanged ChangeVersion.
- Task Key: `CORE-PHYSICAL-OPENING-TARGET-STRUCTURAL-FRESHNESS`

## Confirmed defect

`PhysicalOpeningCutTargetStateCodec.Resolve(...)` validates project IDs and the exact host instance before enumerating caller-provided opening IDs, captures `ProjectState.ChangeVersion`, materializes/normalizes the target IDs, and then revalidates project IDs plus host identity. However it does not pin the target opening instances that existed before caller enumeration. Because `ProjectState.Elements` is publicly mutable, a lazy `openingIds` sequence can replace a Door/WallOpening with a new same-ID instance without calling `Touch()`. The post-enumeration ID validation passes and `Resolve(...)` returns the replacement opening, silently changing physical-cut ownership under the unchanged revision.

## Reserved scope

- `src/QS3D.Core/Services/PhysicalOpeningCutTargetStateCodec.cs`
- `tests/QS3D.Core.SmokeTests/PhysicalOpeningCutTargetStructuralFreshnessSmoke.cs`
- this claim file

## Intended contract

- Snapshot exact ordered project element references before caller target-ID enumeration.
- Preserve the existing `ChangeVersion` freshness check and target normalization/canonicalization behavior.
- After target enumeration, reject add/remove/reorder/same-ID replacement structural drift even when `ChangeVersion` is unchanged, before resolving physical opening targets.
- Resolve target openings from the validated snapshot/current identical structure; keep existing Door/WallOpening category and canonical HostWallId relation checks.
- Preserve host ownership checks, target-state codec serialization/canonical Base64 rules, target count bounds and public result ordering.
- Do not modify physical-cut native code, `HostLinkService`, `ProjectState` collections or persistence.

## Validation plan

Add focused auto-registered Core smoke coverage where a lazy target-ID source replaces an opening with a new same-ID Door linked to the same host without `Touch()`. Resolution must fail structural freshness while `ChangeVersion` remains unchanged. Include a stable canonical target control.

## Validation boundary

No GitHub Actions will be dispatched. No local .NET/full executable smoke or licensed BricsCAD V25/V26 runtime PASS will be claimed unless actually executed.
