# Work claim — Full regeneration dependency integrity

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T10:04:00+07:00`
- Completed: `2026-08-12T10:08:00+07:00`
- Baseline main SHA observed before claim: `17717bdb444d385a8954dbe09e16638bddf34e4b`
- Claim commit: `c7fdefbe8fff1d2c76c41bda429989f31788814c`
- Source commit on branch: `65ed6ee9c2ec1f031af8fec1a4c573ae4b249623`
- Regression-source commit on branch: `4518f3617be957db73ea62de5f98b70d9728dcd5`
- Pull request: `#736`
- Squash merge commit: `4a34f6b1e99218983d5e828ff130d8499cc0a16e`
- Priority: evidence-driven Core full-project execution integrity

## Confirmed defect

`RegenerationEngine.MarkChanged(...)` rebuilds `DependencyGraph` before mutation, and `DependencyGraph.Rebuild(...)` rejects blank, non-canonical, duplicate and dangling semantic dependency references across the full project. `RegenerationEngine.RegenerateDirty(...)` previously validated only null/duplicate project element identities before passing the full `project.Elements` collection to `TopologicalDirtyOrder(...)`.

`TopologicalDirtyOrder(...)` intentionally permits dependencies outside the dirty subset, so it cannot distinguish a clean in-project dependency from a dependency whose target is missing from the project. Full-project `RegenerateDirty()` could therefore silently regenerate a dirty dependent even when its semantic dependency target did not exist.

## Implemented

- `RegenerateDirty()` preserves its existing null/duplicate project-element validation, then rebuilds the full dependency graph once before transactional regeneration.
- Full-project blank, non-canonical, duplicate and dangling dependency states now use the existing `DependencyGraph.Rebuild(...)` fail-closed contract.
- `TopologicalDirtyOrder(...)` was not changed.
- `RegenerateDirtySubset(...)` was not changed, preserving the intentional subset contract where a target may depend on a clean element outside the selected subset.

## Regression source

`RegenerationDirtyDependencyIntegritySmoke` covers:

- a dirty dependent whose target is absent from the project: exact dependency-integrity rejection before project/dirty/quantity mutation;
- a dirty dependent whose dependency exists in the project but is clean: normal regeneration remains allowed and the clean source remains clean.

## Integration evidence

- While the branch was open, `main` advanced 14 commits, but `RegenerationEngine.cs` retained exact pre-patch blob SHA `ce7bbec8469682df9d0271cd90fad2e0497a9475`; no concurrent source overlap was present.
- PR `#736` was squash-merged with expected head SHA `4518f3617be957db73ea62de5f98b70d9728dcd5` into `4a34f6b1e99218983d5e828ff130d8499cc0a16e`.
- Source and regression were read back directly from `main` after merge.

## Validation boundary

Remote/static source + regression review only. No GitHub Actions/build/release was dispatched, smoke source was not executed in this web session, and no BricsCAD V25/V26 or local .NET runtime PASS is claimed.
