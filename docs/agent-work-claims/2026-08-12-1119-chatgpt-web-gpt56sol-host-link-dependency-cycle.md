# Work claim — Host link dependency-cycle preflight

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-host-link-dependency-cycle-20260812-1119`
- Registered: `2026-08-12T11:19:00+07:00`
- Baseline main SHA: `6b2da3495aca6bced29937ff8683da32c2c1fb88`
- Priority: owner-requested continue-all Core mutation integrity

## Confirmed defect

`HostLinkService.LinkOpening(...)` persists the host relation as `opening.DependsOn.Add(wall.Id)` after resolving the target wall, but it does not preflight whether that wall already depends directly or transitively on the opening. A valid acyclic project can therefore be mutated into a dependency cycle (for example `WALL -> OPENING`, followed by host-link `OPENING -> WALL`). The write/audit succeeds because cycle detection only occurs later when dependency ordering is evaluated.

## Reserved scope

- Preflight the proposed host dependency edge before any host-link mutation/audit.
- Reject a target wall that is already a direct/transitive dependent of the opening.
- Preserve canonical same-host repair, re-host physical-cut safety, dependency cleanup, dirty flags, audit-owned revision semantics and unlink behavior.
- Add focused CAD-independent Core smoke coverage proving cycle rejection is atomic and a normal acyclic host link remains valid.

## Expected surfaces

- `src/QS3D.Core/Services/HostLinkService.cs`
- `tests/QS3D.Core.SmokeTests/HostLinkDependencyCycleSmoke.cs`
- this claim file

## Excluded scope

- No changes to `DependencyGraph.cs` or dependency ordering semantics.
- No changes to `UnlinkOpening`, physical opening-cut state, Auto Host metadata policy, CAD/native runtime or UI wrappers.
- No GitHub Actions, force push, release publication or BricsCAD runtime PASS claim.

## Validation plan

- Refresh `main` and re-fetch exact `HostLinkService.cs` after claim registration.
- Build the current dependency graph before mutation and fail if the target wall is already in the opening's transitive dependents; this means adding `opening -> wall` would close a cycle.
- Add a module-initializer smoke with a direct/transitive cycle candidate and assertions that relation, dependencies, project version and audit history remain unchanged; include a canonical acyclic control.
- Re-fetch final source/test, verify ancestry against moving `main`, then close this claim with exact SHAs.

## Completion condition

Completed only when `HostLinkService.LinkOpening(...)` cannot introduce a semantic dependency cycle, focused regression coverage is committed, and this claim is closed on `main` with exact integration evidence.
