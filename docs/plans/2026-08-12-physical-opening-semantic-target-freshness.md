# Plan — Physical opening semantic target freshness

## Problem

`PhysicalOpeningCutTargetStateCodec.Resolve(...)` validates the current project/host, then executes caller-controlled target enumeration through `Normalize(openingIds)`. Structural revalidation after enumeration catches removed/replaced elements and host detachment, but semantic project changes recorded by `ProjectState.ChangeVersion` can pass through when object identities remain stable.

## Contract

1. Capture `project.ChangeVersion` immediately before `Normalize(openingIds)`.
2. After `Normalize(...)` returns, compare the current version with the captured version.
3. On mismatch, fail closed before empty-target handling and before resolving any opening relation.
4. Preserve the existing `ValidateProjectElements(...)`, canonical-host identity recheck, opening category checks, and canonical `HostWallId` ownership checks.
5. Stable lazy target inputs continue to resolve normally.
6. A lazy target input that touches the project and yields no ids fails on semantic freshness rather than proceeding to an empty-target result.

## Regression

Focused Core smoke covers:
- stable lazy target resolution;
- `project.Touch()` followed by a valid opening id fails closed;
- `project.Touch()` followed by an empty sequence fails closed.

Static preflight locks capture/enumerate/check ordering and preservation of the structural/global identity guards.

## Validation

Connector readback confirms committed source/test/preflight content only. No Core executable, Python preflight, GitHub Actions, or licensed BricsCAD runtime PASS is claimed unless actually executed.
