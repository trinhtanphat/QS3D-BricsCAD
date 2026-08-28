# MEP quantity deterministic Count stability

Lane-Key: `issue-4377`

## Boundary

`MepQuantityService.Aggregate` accepts both pure streaming `IEnumerable<MepElement>` sources and sources that expose deterministic cardinality through `ICollection<MepElement>`, `IReadOnlyCollection<MepElement>`, or non-generic `ICollection`.

When deterministic Count evidence is available, aggregation treats it as an integrity contract rather than a sizing hint:

1. read all supported Count surfaces before traversal;
2. reject negative, conflicting, or over-limit initial evidence before enumeration;
3. reject the first item beyond the admitted Count before null/duplicate/grouping work for that item;
4. reject under-yield after traversal;
5. re-read all supported Count surfaces after caller-controlled traversal and reject changed, negative, conflicting, or over-limit evidence before aggregate result publication.

Pure streaming sources do not gain a synthetic Count contract. They retain the independent maximum of 10,000 traversed elements.

## Deterministic regression

`tests/QS3D.Core.SmokeTests/MepQuantityInputBoundSmoke.cs` covers:

- pre-enumeration oversize, negative and conflicting Count rejection;
- exact traversal followed by Count drift;
- post-traversal multi-interface Count conflict;
- post-traversal negative Count evidence;
- stable two-phase Count evidence with exactly one enumeration;
- pure streaming acceptance and the 10,000-item streaming ceiling;
- existing duplicate/null validation and exact-boundary acceptance.

`python scripts/preflight-mep-quantity-known-count-stability.py` locks the two-phase source/test structure and is auto-discovered by aggregate preflight.

## Validation

Repository-safe validation is sufficient for this Core-only contract:

```text
python scripts/preflight-mep-quantity-known-count-stability.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

Shared branch CI and protected PR `preflight` + `core` remain mandatory before merge. Licensed BricsCAD, private DWG and LOCAL_PASS evidence are not applicable to this lane.
