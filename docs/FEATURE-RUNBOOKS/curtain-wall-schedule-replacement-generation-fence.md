# Curtain Wall schedule replacement generation fence

## Scope

This REMOTE_SAFE package hardens `CurtainWallScheduleBuilder` against source-generation drift when `ProjectState.Elements` is changed by replacing an element instance without calling a project mutation helper.

`ProjectState.Floors` and `ProjectState.Families` are mutation-aware catalogs and already advance `ChangeVersion`. `ProjectState.Elements` is currently a plain `List<ProjectElement>`, so replacing a slot with a distinct but semantically equivalent `ProjectElement` does not itself advance the project revision.

## Defect and invariant

Before this package the Curtain Wall schedule revision snapshot retained detached semantic values only. A replacement element carrying the same id, references, timestamp, quantities, and source handles could therefore satisfy both `ChangeVersion` and semantic snapshot checks even though the source object generation had changed.

The schedule now freezes both:

- the original `ProjectElement` references used to establish the source generation; and
- detached immutable `CurtainElementSnapshot` values used for aggregation.

Every revision checkpoint first proves that the live element list still contains the same instances in the same positions, then proves semantic equality. Aggregation remains detached from the live source objects.

## Deterministic regression

`CurtainWallScheduleReplacementGenerationFenceSmoke` proves two cases:

1. unchanged same-instance project state still builds a Curtain Wall schedule normally; and
2. a distinct equivalent element replacement that leaves `ProjectState.ChangeVersion` unchanged is rejected by the revision fence.

The malformed replacement fixture intentionally preserves the original `UpdatedUtc` through test-only reflection so the regression isolates instance-generation identity rather than timestamp drift.

## Validation

Run the focused source guard:

```text
python scripts/preflight-curtain-wall-schedule-replacement-generation-fence.py
```

Run deterministic Core smoke coverage:

```text
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

The merge candidate must also pass the repository's protected Shared `preflight` and `core` checks on the exact reconciled head after the latest protected `main` and collision/freshness checks are refreshed.

No licensed BricsCAD runtime is required by this package, and no `LOCAL_PASS` claim is made.