# Grid renumber known-Count stability

## Scope

This runbook qualifies deterministic Core integrity for `GridNamingService.Renumber`. The input sequence is caller-controlled and may also expose deterministic cardinality through `ICollection<string>`, `IReadOnlyCollection<string>`, or non-generic `ICollection`.

No BricsCAD host, private DWG, or licensed runtime is required.

## Defect boundary

Before this package, Grid renumbering bound Count before enumeration and compared only the final number of traversed ids. A hostile counted enumerable could advertise `N`, yield exactly `N` valid Grid ids, mutate its Count during traversal, and still continue into target resolution/planning/mutation with stale cardinality evidence.

The Count getters are also caller-controlled code. Grid renumber already pins `ProjectState.ChangeVersion` around initial Count observation and input traversal; post-traversal Count rebinding must preserve that same anti-race boundary so a Count getter cannot mutate the Project and then allow Grid labels to be changed.

## Hardened contract

`GridNamingService.Renumber` now:

1. binds every supported deterministic Count surface before traversal;
2. rejects negative, conflicting, and over-limit admission evidence;
3. rejects the first item beyond an admitted Count before retaining that extra id;
4. preserves exact under-yield rejection after traversal;
5. rebinds all supported Count surfaces after traversal;
6. rejects negative, conflicting, disappeared, or changed post-traversal Count evidence before target resolution/planning/mutation;
7. rechecks `ProjectState.ChangeVersion` after the post-traversal Count read, before any Grid mutation;
8. preserves the independent 2,000-item streaming ceiling for sources with no deterministic Count surface.

## Deterministic regression

`tests/QS3D.Core.SmokeTests/GridNamingKnownCountStabilitySmoke.cs` auto-runs through a module initializer and covers:

- generic Count drift;
- read-only Count drift;
- non-generic Count drift;
- negative post-traversal Count;
- cross-interface Count conflict after traversal;
- a post-traversal Count getter that mutates `ProjectState`;
- atomic preservation of existing Grid label/sequence values on rejected unstable evidence;
- stable counted input;
- pure streaming input.

`scripts/preflight-grid-naming-known-count-stability.py` pins ordering so Count rebinding and project-version validation occur after exact traversal and before target planning or mutation.

## Repository-safe validation

Run normal Shared Branch and protected PR CI on the exact candidate. Merge only after current-main reconciliation and terminal protected `preflight` + `core` success for the exact head.

This package does not claim licensed BricsCAD runtime, private-DWG evidence, or `LOCAL_PASS`.
