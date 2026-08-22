# QS3D deterministic regeneration work profile

Updated: 2026-08-11

Status: `REMOTE_DONE` for the bounded Core diagnostic contract in this batch. Exact BricsCAD V25 runtime timing, native regeneration performance, private-DWG stress behavior and machine-specific profiling remain `LOCAL_ONLY`.

## Purpose

`RegenerationWorkProfiler` provides a read-only description of the **initial work shape** that the current semantic regeneration engine would inspect for a project or an explicit subset.

It exists to answer source-safe questions such as:

- how many dirty elements are in the project;
- how many dirty elements are in the requested regeneration candidate set;
- which stable semantic element IDs appear in the initial topological dirty order;
- how many dependency edges are internal to that planned dirty set;
- what the maximum dependency depth is;
- which element categories dominate the planned set;
- how many planned dirty elements have semantic dirty flags versus geometry-only dirty flags.

The profile intentionally **does not benchmark elapsed time**, estimate milliseconds, or claim native BricsCAD performance. Those measurements require a qualified runtime host and representative DWGs.

## Engine alignment

The profiler uses the same `DependencyGraph.TopologicalDirtyOrder(...)` contract used by `RegenerationEngine`.

For subset profiling it mirrors the current `RegenerateDirtySubset` target boundary:

- an empty subset is valid and produces zero planned work;
- blank IDs fail closed;
- surrounding whitespace fails closed instead of being silently normalized;
- duplicates are rejected case-insensitively;
- every requested ID must exist in the current project;
- requested elements are resolved in project order before topological dirty ordering.

`TargetElementIds` in the returned profile are sorted case-insensitively for deterministic presentation. `Items` retain the actual topological dirty order used to describe the candidate work.

## Metrics

Each `RegenerationWorkItem` contains only semantic/source-safe data:

- stable `ElementId`;
- `ElementCategory`;
- current `ElementDirtyFlags`;
- initial topological order index;
- dependency depth within the planned dirty candidate set;
- direct planned dependency count;
- direct planned dependent count.

The aggregate profile additionally exposes:

- project element count;
- dirty project element count;
- planned element count;
- semantic-dirty planned count;
- geometry-only dirty planned count;
- internal dependency edge count;
- maximum dependency depth;
- category breakdown.

Dependencies outside the selected dirty candidate set do not contribute to the internal edge/depth metrics. This matches the bounded question being measured rather than pretending the subset is a complete project graph.

## Read-only boundary

Profiling:

- does not invoke regenerators;
- does not call `RegenerateDirty` or `RegenerateDirtySubset`;
- does not run quantity rules;
- does not mark elements clean or dirty;
- does not call `ProjectState.Touch()`;
- does not open a CAD transaction;
- does not capture or expose native handles.

The profiler records `SourceChangeVersion` and fails if the project change version moves while the profile is being assembled.

## Initial-pass semantics

`RegenerationEngine` may execute more than one semantic regeneration pass because a regenerator or rule can affect later dirty state. The source-only profile deliberately does not simulate those mutations. Therefore `PlannedElementCount` means **initial dirty candidate work**, not “guaranteed number of regenerated elements” and not a final pass count.

Geometry-only dirty elements remain visible in the profile because they are present in `TopologicalDirtyOrder`, while `HasSemanticDirtyWork` separately reflects the semantic dirty flags that the current engine checks before invoking semantic regenerators/rules.

## Regression coverage

`RegenerationWorkProfileSmoke` covers:

- deterministic project profiling;
- no project/element dirty or timestamp mutation;
- subset target ordering and topology;
- geometry-only versus semantic dirty visibility;
- malformed/unknown target rejection;
- a deterministic 2,048-element dependency chain to keep the profiling path iterative and bounded away from recursive-stack behavior.

`scripts/preflight-regeneration-work-profile.py` is auto-discovered by `scripts/preflight-all.py` and guards the source-only/read-only boundary plus subset target-contract alignment with `RegenerationEngine`.

## Qualification boundary

Still `LOCAL_ONLY`:

- exact-SHA BricsCAD V25 NETLOAD/runtime execution;
- elapsed-time and memory profiling on licensed Windows V25;
- native geometry/boolean/rebar generation cost;
- realistic large/private DWG workload measurements;
- multi-document runtime behavior;
- UI responsiveness and cancellation behavior;
- hardware-specific performance thresholds.

Do not turn these deterministic Core counts into runtime speed claims without those measurements.
