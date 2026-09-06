# Project Quantity report semantic generation fence

Issue: #5906  
Lane-Key: `issue-5906`

## Contract

Project Quantity reporting must publish a row set from one semantic project generation. The builder therefore captures detached immutable values for the project identity/fingerprint, element relations/properties/quantities/provenance and the Floor/Zone/Family catalog values consumed by the report. Aggregation reads only those frozen values.

The live project is revalidated before and during aggregation and again before publication. A direct element replacement or an in-place mutation to consumed quantity, property, catalog or provenance state must fail closed even when `ProjectState.ChangeVersion` remains unchanged.

Selection contracts, room-finish exclusion, grouping/detail behavior, evidence flags, compensated aggregation, density/mass calculations and diagnostics remain unchanged.

## Deterministic validation

Run the managed smoke suite:

```text
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

The ModuleInitializer regression `ProjectQuantityReportGenerationFenceSmoke` verifies a stable generation and then injects in-place quantity, Family-name and SourceHandles drift immediately after capture. Each drift case must throw the standard recompute diagnostic while proving `ProjectState.ChangeVersion` did not change.

Run the focused source guard:

```text
python scripts/preflight-project-quantity-report-generation-fence.py
```

The focused preflight pins frozen aggregation, copied semantic dictionaries/lists/provenance, repeated generation checks and the deterministic regression cases. Fresh protected `preflight` and `core` checks on the exact candidate SHA are required before merge.

## Runtime boundary

This is managed Core reporting correctness. Licensed BricsCAD runtime acceptance is not applicable and no remote `LOCAL_PASS` claim is permitted or required for this carrier.
