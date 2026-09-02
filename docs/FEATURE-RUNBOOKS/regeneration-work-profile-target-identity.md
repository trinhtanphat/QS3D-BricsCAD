# Regeneration work-profile target identity integrity

Issue: #5303
Lane-Key: `issue-5303`
Runtime: deterministic Core only; no licensed BricsCAD evidence is required.

## Defect

`RegenerationWorkProfile` already canonicalized `ProjectId`, and each `RegenerationWorkItem` canonicalized its `ElementId`, through `RegenerationWorkIdentityContract`. `TargetElementIds`, however, crossed only the generic bounded collection materializer. That preserved collection cardinality and null integrity but allowed blank, control-bearing, malformed UTF-16, XML-invalid, or non-canonical surrounding-whitespace target identities to enter immutable regeneration evidence.

## Contract

`TargetElementIds` are first detached through the existing bounded/known-Count-safe materializer. After caller-controlled enumeration has completed and its Count contract has been rebound, each detached target id is admitted through `RegenerationWorkIdentityContract.Require` before immutable publication.

The target identity boundary therefore:

- preserves existing collection bounds, known-Count stability, null-entry precedence, counted-source behavior, and pure-streaming behavior;
- rejects blank identities, control characters, malformed UTF-16, and XML-invalid text;
- trims surrounding whitespace once, matching the existing work-item/project identity contract;
- preserves case and valid supplementary-plane Unicode exactly;
- does not introduce new duplicate-target semantics.

## Deterministic validation

Run from the repository root:

```text
python scripts/preflight-regeneration-work-profile-target-identity.py
python scripts/preflight-regeneration-work-profile-identity.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
dotnet build src/QS3D.Core/QS3D.Core.csproj -c Release
```

The registered smoke covers canonical target trimming, mixed-case preservation, supplementary-plane Unicode, blank/control identities, isolated high/low surrogates, and XML-invalid noncharacters while retaining the historical project/work-item identity regressions.
