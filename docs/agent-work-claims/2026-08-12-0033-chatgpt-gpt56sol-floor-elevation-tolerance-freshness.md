# Work claim — Floor elevation tolerance freshness

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-gpt56sol-20260812-floor-elevation-tolerance-freshness`
- Registered: `2026-08-12T00:33:34+07:00`
- Last Updated: `2026-08-12T00:38:10+07:00`
- Baseline main SHA: `935bab2c0e2224429909a2838a83006cf215d29a`
- Priority: deterministic Core freshness leak found during owner-requested continue-all audit
- Task Key: `CORE-FLOOR-ELEVATION-TOLERANCE-FRESHNESS`
- Implementation PR: `#580`
- Implementation commit on `main`: `4d41ec7221e5c375a4f2ce1542f331626770a788`

## Confirmed defect

`ProjectFloorService.Update(...)` intentionally treats tiny elevation deltas within `NearlyEqual(...)` as an elevation no-op. Before this fix, when a Floor name changed in the same call, the method still executed `floor.ElevationM = elevationM` even when `elevationChanged == false`. Referencing elements were then dirtied only for `Relations | Quantity`, not `Geometry`.

That allowed a numerical Floor elevation mutation to be persisted while the same branch simultaneously classified it as a non-geometry change.

## Implemented scope

The existing tolerance is now a true no-op threshold: `FloorDefinition.ElevationM` is assigned only when `elevationChanged` is true. A rename combined with a sub-tolerance requested elevation preserves the exact stored elevation while keeping the existing name-change relation/quantity dirty behavior. Material elevation changes still update the value and add `Geometry` dirtiness.

Focused isolated Core smoke coverage verifies:

- rename + sub-tolerance elevation preserves exact stored elevation and does not introduce Geometry dirty;
- materially different elevation updates exactly and marks referenced geometry dirty;
- pure sub-tolerance elevation request with unchanged name remains a complete `ChangeVersion`/element-freshness no-op.

## Surfaces changed

- `src/QS3D.Core/Domain/ProjectFloorService.cs`
- `tests/QS3D.Core.SmokeTests/ProjectFloorElevationToleranceSmoke.cs`
- `tests/QS3D.Core.SmokeTests/ProjectFloorElevationToleranceSmokeRegistration.cs`
- this claim file

## Coordination / exclusions preserved

- The completed Floor/Zone mutation-integrity source/preflight/smoke lane was not modified.
- `ProjectZoneService`, persistence schema, BricsCAD adapter/UI and geometry planners were not changed.
- The existing `NearlyEqual(...)` tolerance was not removed or retuned.
- No GitHub Actions/build/release workflow was dispatched and no LOCAL_ONLY runtime PASS is claimed.

## Validation evidence

- Claim was published on `main` before implementation at `720ae817cfd0036e316f9797a0c8e4bd9029394d`.
- Post-claim re-fetch confirmed `ProjectFloorService.cs` blob `e4f1534319b0b69c4034055ef1eea626db1e0a5c` still contained the inconsistent unconditional elevation assignment.
- PR `#580` diff was reviewed before merge: one source-line behavior change plus two new isolated smoke/registration files (`3 files`, `+106/-1`).
- Server-side squash merge produced `4d41ec7221e5c375a4f2ce1542f331626770a788`.
- Merge commit read-back confirms only the conditional elevation assignment and the focused three-case smoke/registration were added.
- Local build/smoke execution is **not** claimed because this connector-only environment does not provide the repository checkout/build runner.

## Completion

`COMPLETED`: current `main` no longer persists a Floor elevation delta that `ProjectFloorService` classified as non-changing for geometry freshness, while real elevation changes retain their existing update/dirty semantics.
