# MEP quantity deterministic Count stability

Current strengthening lane: `issue-4636` (building on the earlier `issue-4377` two-phase Count contract).

## Boundary

`MepQuantityService.Aggregate` accepts both pure streaming `IEnumerable<MepElement>` sources and sources that expose deterministic cardinality through `ICollection<MepElement>`, `IReadOnlyCollection<MepElement>`, or non-generic `ICollection`.

When deterministic Count evidence is available, aggregation treats it as an integrity contract rather than a sizing hint:

1. read all supported Count surfaces before traversal;
2. reject negative, conflicting, or over-limit initial evidence before enumeration;
3. re-bind all supported Count surfaces immediately before every caller-controlled `MoveNext`;
4. after a successful `MoveNext`, re-bind Count again before observing `IEnumerator.Current`;
5. reject the first item beyond the admitted Count before `Current`, null/duplicate validation, or grouping work for that item;
6. reject under-yield after traversal;
7. re-bind Count once more after traversal and reject changed, negative, conflicting, missing, or over-limit evidence before aggregate result publication.

This stronger ordering prevents a hostile counted source from changing Count transiently during traversal and restoring the admitted value before a final-only check. The integrity order for a known-count source is therefore `Count -> MoveNext -> Count -> Current`, repeated for each admitted item.

Pure streaming sources do not gain a synthetic Count contract. They retain the independent maximum of 10,000 traversed elements.

## Deterministic regression

`tests/QS3D.Core.SmokeTests/MepQuantityInputBoundSmoke.cs` covers:

- pre-enumeration oversize, negative and conflicting Count rejection;
- exact traversal followed by Count drift;
- post-traversal multi-interface Count conflict;
- post-traversal negative Count evidence;
- stable counted input with the stronger re-bind cadence;
- pure streaming acceptance and the 10,000-item streaming ceiling;
- existing duplicate/null validation and exact-boundary acceptance.

`tests/QS3D.Core.SmokeTests/MepQuantityMidTraversalCountDriftSmoke.cs` adds adversarial traversal instrumentation and proves:

- Count drift before an advancement fails before that `MoveNext`;
- Count drift caused by a successful advancement fails before the corresponding `Current`;
- a transient Count change cannot disappear before publication and escape detection;
- stable counted input consumes exactly the admitted `Current` values while retaining a single terminal `MoveNext`.

`tests/QS3D.Core.SmokeTests/MepQuantityKnownCountOverrunSmoke.cs` continues to pin known-count overrun precedence before unexpected item validation.

`python scripts/preflight-mep-quantity-known-count-stability.py` locks the source/test ordering and is auto-discovered by aggregate preflight.

## Validation

Repository-safe validation is sufficient for this Core-only contract:

```text
python scripts/preflight-mep-quantity-known-count-stability.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

Shared branch CI and protected PR `preflight` + `core` remain mandatory before merge. Licensed BricsCAD, private DWG and LOCAL_PASS evidence are not applicable to this lane.
