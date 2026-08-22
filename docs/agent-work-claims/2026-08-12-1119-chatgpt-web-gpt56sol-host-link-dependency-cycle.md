# Work claim — Host link dependency-cycle preflight

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-host-link-dependency-cycle-20260812-1119`
- Registered: `2026-08-12T11:19:00+07:00`
- Completed: `2026-08-12T11:44:00+07:00`
- Baseline main SHA: `6b2da3495aca6bced29937ff8683da32c2c1fb88`
- Priority: owner-requested continue-all Core mutation integrity

## Confirmed defect

`HostLinkService.LinkOpening(...)` persisted the host relation as `opening.DependsOn.Add(wall.Id)` after resolving the target wall, but did not preflight whether that wall already depended directly or transitively on the opening. A valid acyclic project could therefore be mutated into a dependency cycle, with cycle detection deferred until later dependency ordering.

## Completed implementation

- Claim commit: `cc44bde30e4f685774c59186ba2a768dc4aaaae3`
- Product fix: `66c15ba42b6c83c06d64a4de63674df0d967131a`
- Regression: `0a075eacbb9781bd4a782caaa17499abd8f061f4`
- `HostLinkService.LinkOpening(...)` now preflights the target wall dependency closure before adding a new semantic host dependency edge.
- Direct/transitive dependency paths from the wall back to the opening fail before relation, dependency, audit or revision mutation.
- Closure traversal fails closed for blank, padded or missing dependency identities in the traversed subgraph.
- Existing same-host canonical repair behavior remains outside the new-edge cycle preflight.
- Focused CAD-independent smoke coverage pins a transitive cycle rejection as atomic and preserves an acyclic control.

## Reserved scope

- `src/QS3D.Core/Services/HostLinkService.cs`
- `tests/QS3D.Core.SmokeTests/HostLinkDependencyCycleSmoke.cs`
- this claim file

## Excluded scope

- No changes to `DependencyGraph.cs` or dependency ordering semantics.
- No changes to `UnlinkOpening`, physical opening-cut state, Auto Host metadata policy, CAD/native runtime or UI wrappers.
- No GitHub Actions, force push, release publication or BricsCAD runtime PASS claim.

## Validation evidence

- Final regression commit `0a075eacbb9781bd4a782caaa17499abd8f061f4` is an ancestor of refreshed `main` `099e3fd46758e3d2c16c05e833016bdcf1aab8e9` (`ahead_by=77`, `behind_by=0`, merge base equals the regression commit).
- Final source and smoke were re-fetched after integration in the implementation session; subsequent concurrent changes observed before closure did not modify the reserved source/test paths.
- Compile-surface readback confirmed `ElementCategory.CustomQuantity` and the `ProjectElement(id, category, familyId, floorId, zoneId)` constructor used by the focused smoke exist in the repository.
- The smoke file was committed but not executed in this GitHub connector session; no build, GitHub Actions or BricsCAD runtime PASS is claimed.

## Completion condition

Completed: `HostLinkService.LinkOpening(...)` cannot introduce the covered direct/transitive semantic dependency cycle, focused regression coverage is integrated on `main`, and this claim is closed with exact commit ancestry evidence.
