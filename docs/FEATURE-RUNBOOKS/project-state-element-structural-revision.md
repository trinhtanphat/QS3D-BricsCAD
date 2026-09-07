# ProjectState element structural revision

Status: REMOTE_SAFE deterministic Core/domain qualification.

## Defect

`ProjectState.Elements` historically used a plain `List<ProjectElement>` while catalog collections used mutation-aware wrappers. Structural element-set changes could therefore alter project generation without advancing `ProjectState.ChangeVersion`.

## Required behavior

Every real structural mutation through `ProjectState.Elements` (`Add`, `Insert`, index replacement, `Remove`, `RemoveAt`, `Clear`) must advance the owning project revision exactly once before the collection is mutated. Replacing an index with the same object instance, removing a missing item, or clearing an empty collection must not churn revision state. If `ChangeVersion` cannot advance, the structural mutation must not occur.

This package intentionally does not claim that arbitrary in-place edits made directly through a `ProjectElement`'s public nested dictionaries/lists are project-revision aware. That is a distinct semantic-mutation boundary.

## Deterministic qualification

Run:

```text
python scripts/preflight-project-state-element-structural-revision.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

The focused guard pins ownership by `StructuralRevisionList<ProjectElement>(Touch)`, fail-before-write ordering, no-op same-instance replacement, smoke registration, and removal of the historical Curtain Wall expectation that equivalent replacement could leave `ChangeVersion` unchanged.

The smoke covers structural mutation revision deltas, no-op cases, and checked-overflow atomicity.

## Runtime boundary

No licensed BricsCAD execution is needed. Protected Shared `preflight` and `core` on the exact candidate are authoritative for this managed Core package. Merge remains subject to current-main freshness, Reservation-v2 collision cleanliness, review cleanliness, and expected-head guarded integration.
