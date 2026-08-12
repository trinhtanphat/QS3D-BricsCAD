# Work claim — Stale Auto Host metadata freshness

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-host-auto-metadata-freshness`
- Registered: `2026-08-12T08:51:00+07:00`
- Baseline main SHA: `89cac50d41bb3c9efb796f79b94ac94338e05b2a`
- Priority: P1 — persisted element freshness must advance when stale Auto Host provenance is removed.

## Confirmed defect

`HostLinkService.UnlinkOpening(...)` has a repair branch for an opening/door that no longer contains `HostWallId` but still carries stale `AutoHostMatched`, `AutoHostGapM`, `AutoHostElevDeltaM` and/or `AutoHostCandidateCount` metadata. The branch removes those persisted element properties and records an audit event, but it never updates `ProjectElement.UpdatedUtc`. The project revision advances through the audit record while the element freshness timestamp still describes the pre-cleanup persisted state.

Other Host Link mutation paths call `MarkDirty(...)` after metadata/property changes and therefore already advance the element timestamp. This lane only covers the cleanup-only absent-host branch and does not alter dirty/generated semantics.

## Reserved scope

- `src/QS3D.Core/Services/HostLinkService.cs`
- `tests/QS3D.Core.SmokeTests/HostLinkAutoMetadataFreshnessSmoke.cs`
- `tests/QS3D.Core.SmokeTests/HostLinkAutoMetadataFreshnessRegistration.cs`
- this claim file

## Intended contract

- Removing stale `AutoHost*` metadata without `HostWallId` advances `opening.UpdatedUtc` exactly as a persisted element-state change.
- The cleanup-only repair does not mark geometry/relations/quantity dirty and does not invent a host relation.
- If neither `HostWallId` nor stale Auto Host metadata exists, unlink remains a true no-op with unchanged element/project freshness and audit count.
- Existing linked/re-host/unlink behavior remains unchanged.

## Excluded scope

- No changes to Auto Host candidate matching, V25 command lifecycle, physical opening-cut safety, generated geometry, host dependency semantics, audit normalization, or QSDB persistence format.
- No GitHub Actions dispatch and no BricsCAD V25/V26 runtime qualification claim.

## Validation plan

- Re-fetch `HostLinkService.cs` after claim publication and write against its exact blob SHA.
- Add focused module-initializer Core smoke covering cleanup-only timestamp advancement, dirty-flag preservation, metadata removal/audit/project revision and empty absent-host no-op behavior.
- Review exact pushed diff and read back final source/test from current `main`.
- Close claim with exact commit SHAs and verify ancestry without force-push.
- No compile/test-runtime PASS will be claimed unless actually executed.

## Coordination

The historical Host Link atomicity/canonicalization/audit lanes are completed. Current Auto Host single-bind/post-commit claims reserve V25 command/preflight surfaces, not `HostLinkService.cs`. Recent active repository lanes observed before registration are otherwise disjoint.

## Completion condition

Cleanup-only stale Auto Host metadata repair advances persisted element freshness without adding dirty semantics, focused regression source is on `main`, and this claim is marked `COMPLETED` with truthful validation notes.
