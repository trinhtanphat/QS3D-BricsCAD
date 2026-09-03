# QS3D Review workbook Count-channel integrity

## Scope

`Qs3dReviewWorkbookExporter` snapshots caller-owned `IReadOnlyList<T>` inputs before validating or publishing workbook state. A source may also expose generic or non-generic collection Count channels. Those channels are part of the same caller-visible cardinality contract and must not disagree or drift independently.

## Contract

- Bind the admitted `IReadOnlyList<T>.Count` and every available `ICollection<T>.Count` / `ICollection.Count` channel before traversal.
- Reject negative or conflicting Count evidence before `MoveNext` or `Current`.
- Revalidate every admitted Count channel before and after caller-controlled `MoveNext`, immediately after `Current` and before retention, after terminal traversal, and before returning the detached snapshot.
- Preserve the existing no-overread rule: once the admitted cardinality is exhausted, reject before reading another `Current`.
- Preserve exact under-yield detection and stable pure-`IReadOnlyList<T>` behavior.

## Deterministic regression

`Qs3dReviewWorkbookCountNoOverreadSmoke` covers the historical read-only Count drift cases plus:

1. an admission-time conflict where `IReadOnlyList<T>.Count` and `ICollection<T>.Count` disagree before traversal;
2. a hostile `Current` that mutates only the generic collection Count channel, which must fail before the returned value is retained;
3. a stable source exposing all three Count interfaces, which must remain accepted with order/value preservation.

## Validation

Run the focused preflight `scripts/preflight-qs3d-review-workbook-count-channel-integrity.py`, then the repository deterministic Core smoke/build and protected Shared CI. Runtime BricsCAD evidence is not applicable to this Core export-integrity package.
