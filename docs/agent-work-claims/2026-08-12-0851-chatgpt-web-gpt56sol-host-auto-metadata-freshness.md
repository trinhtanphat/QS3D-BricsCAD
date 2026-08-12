# Work claim — Stale Auto Host metadata freshness

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-host-auto-metadata-freshness`
- Registered: `2026-08-12T08:51:00+07:00`
- Completed: `2026-08-12T08:54:00+07:00`
- Baseline main SHA: `89cac50d41bb3c9efb796f79b94ac94338e05b2a`
- Claim commit: `d51ef6e4886074fac550cbd528ebe2ef5c4dfd02`
- Source commit: `a119ab27d5ba6faf31e28650c325c98b0898be20`
- Regression commit: `155bcd4f28ce4102bc9333aa1420ba9f93ee6217`
- Registration commit: `652c1aa17f43489db931098fe766a20ef2670222`
- Priority: P1 — persisted element freshness must advance when stale Auto Host provenance is removed.

## Confirmed defect

`HostLinkService.UnlinkOpening(...)` has a repair branch for an opening/door that no longer contains `HostWallId` but still carries stale `AutoHostMatched`, `AutoHostGapM`, `AutoHostElevDeltaM` and/or `AutoHostCandidateCount` metadata. The branch removed those persisted element properties and recorded an audit event, but it did not update `ProjectElement.UpdatedUtc`. The project revision advanced through the audit record while the element freshness timestamp still described the pre-cleanup persisted state.

Other Host Link mutation paths already call `MarkDirty(...)` after metadata/property changes and therefore advance the element timestamp. This lane only covers the cleanup-only absent-host branch and does not alter dirty/generated semantics.

## Completed scope

- `src/QS3D.Core/Services/HostLinkService.cs`
- `tests/QS3D.Core.SmokeTests/HostLinkAutoMetadataFreshnessSmoke.cs`
- `tests/QS3D.Core.SmokeTests/HostLinkAutoMetadataFreshnessRegistration.cs`
- this claim file

## Resulting contract

- Removing stale `AutoHost*` metadata without `HostWallId` advances `opening.UpdatedUtc` as a persisted element-state change.
- The cleanup-only repair does not mark geometry/relations/quantity dirty and does not invent a host relation or dependency.
- If neither `HostWallId` nor stale Auto Host metadata exists, unlink remains a true no-op with unchanged element/project freshness and audit count.
- Existing linked/re-host/unlink behavior remains unchanged.

## Implementation

The absent-host cleanup lambda now calls `opening.TouchPersistenceState()` only after `ClearAutoHostMetadata(...)` reports that at least one persisted metadata entry was removed. The touch occurs before the existing audit write, so rollback through `ProjectSemanticMutationExecutor` still restores the pre-operation element timestamp if audit recording fails. The helper itself remains unchanged, avoiding double-touching linked/re-host/unlink paths that already call `MarkDirty(...)`.

The focused smoke covers cleanup-only metadata removal, element timestamp advancement, Dirty preservation, no invented HostWallId/dependencies, exactly one project revision/audit event, and the empty absent-host no-op. Registration uses a dedicated module initializer.

## Validation actually performed

- After claim publication, compared claim commit `d51ef6e4886074fac550cbd528ebe2ef5c4dfd02` to moving `main`; 13 intervening commits were disjoint from the reserved source/test paths.
- Re-fetched `HostLinkService.cs` and wrote against exact pre-fix blob `52be392a4477e1858cbdfa7d350dad2226718991` through GitHub Contents API.
- Reviewed exact source commit `a119ab27d5ba6faf31e28650c325c98b0898be20`; the diff is exactly one added `opening.TouchPersistenceState()` call in the cleanup-only branch.
- Read back smoke blob `b5b103f573b6b67297882970ebc206198ed65f86` and registration blob `98966e0ce0499c0d30fc02ad3867ef156871a0b7` from current `main`.
- The timestamp regression waits until the wall clock is later than the baseline before invoking cleanup, avoiding dependence on timestamp resolution while still proving the cleanup path performs a new element touch.
- GitHub Actions were not dispatched.
- No local .NET/Core smoke execution or licensed BricsCAD V25/V26 runtime PASS is claimed from this remote session.

## Excluded scope honored

- No changes to Auto Host candidate matching, V25 command lifecycle, physical opening-cut safety, generated geometry, host dependency semantics, audit normalization, or QSDB persistence format.

## Coordination

The historical Host Link atomicity/canonicalization/audit lanes remain completed. Current Auto Host single-bind/post-commit claims reserve V25 command/preflight surfaces, not `HostLinkService.cs`; concurrent repository changes were preserved.

## Completion

Cleanup-only stale Auto Host metadata repair now advances persisted element freshness without adding dirty semantics, while an empty absent-host unlink remains a true no-op. Focused regression source is on `main`, and the claim is released as completed.
