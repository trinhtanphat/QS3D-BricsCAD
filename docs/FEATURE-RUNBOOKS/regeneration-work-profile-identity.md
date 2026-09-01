# Regeneration work-profile identity integrity

Issue: #5188
Lane-Key: `issue-5188`
Runtime: deterministic Core only; no licensed BricsCAD evidence is required.

## Defect

Before #5188, the public `RegenerationWorkItem` and `RegenerationWorkProfile` constructors only rejected blank `ElementId` / `ProjectId` values. They retained surrounding whitespace and admitted control characters, malformed UTF-16, and XML-invalid text. Public regeneration evidence could therefore carry semantic identities that disagreed with canonical project identity behavior or could not safely round-trip through strict text/XML persistence boundaries.

## Contract

`RegenerationWorkIdentityContract.Require` is the shared public-admission boundary for required regeneration-work identities. It:

- rejects null/blank values;
- trims surrounding whitespace once;
- preserves case and valid Unicode content;
- rejects control characters;
- rejects malformed UTF-16 and XML-invalid characters using `XmlConvert.VerifyXmlChars`.

Both `RegenerationWorkItem.ElementId` and `RegenerationWorkProfile.ProjectId` must use this contract. Numeric, category, materialization, Count-integrity, ordering, and profiling behavior remain unchanged.

## Deterministic validation

Run from the repository root:

```text
python scripts/preflight-regeneration-work-profile-identity.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
dotnet build src/QS3D.Core/QS3D.Core.csproj -c Release
```

The smoke covers surrounding-whitespace canonicalization, mixed-case preservation, valid supplementary-plane Unicode, control characters, isolated high/low surrogates, and XML-invalid noncharacters for both public identity surfaces.
