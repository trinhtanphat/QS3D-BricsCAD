# Plan — Regeneration work profiler structural freshness

## Problem

`RegenerationWorkProfiler.ProfileSubset(...)` guards semantic freshness with `ProjectState.ChangeVersion`, but caller-controlled lazy target enumeration can directly mutate `project.Elements` without advancing that version. Removal or same-ID replacement can therefore change the ownership universe before candidate selection while passing the current freshness gate.

## Contract

1. Snapshot the current project element ownership map immediately before target enumeration, rejecting null or duplicate IDs up front.
2. Capture and retain the existing `ChangeVersion` check.
3. After target enumeration and the semantic-version check, compare the current element collection against the ownership snapshot by count, unique ID and object reference.
4. Fail closed before empty-subset handling and before candidate planning when ownership drifted.
5. Preserve existing unknown-target, duplicate-target, canonical-ID, graph, and profiling behavior.

## Regression

Focused Core smoke covers:
- stable lazy subset still profiles expected elements;
- same-ID replacement during lazy enumeration fails with unchanged `ChangeVersion`;
- removal followed by an empty target sequence fails before the empty-subset profile path.

Static preflight locks snapshot/version/enumeration/recheck ordering and smoke registration.

## Validation

Connector readback confirms committed source/test/preflight content only. No Core executable, Python preflight, GitHub Actions, or licensed BricsCAD runtime PASS is claimed unless actually executed.
