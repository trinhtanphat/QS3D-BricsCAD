# #4373 IFC round-trip known Count stability

Status: `SOURCE_READY / PENDING_BRANCH_CI`

Lane-Key: `issue-4373`

Baseline source audited: `main@11179b617e5924901383e79228b24e0be3199193`.

## Scope

This source package hardens deterministic collection-count evidence across the five IFC collection boundaries that canonicalize export/trace state:

1. `IfcRoundTripProjection.CanonicalizeDimensions`;
2. `IfcRoundTripProjection.CanonicalizeProvenance`;
3. `IfcRoundTripProjectionSet.Create`;
4. `IfcRoundTripQuantityEvidenceSet.Create`;
5. `IfcRoundTripExchangeResultSet.Create`.

The earlier issue-4316 contract already rejects the first yielded item beyond an admitted known Count before semantic processing. #4373 preserves that behavior and closes the remaining post-traversal stability gap.

## Required ordering

For a source exposing deterministic Count metadata, the acceptance sequence is:

1. observe and validate supported `ICollection<T>`, `IReadOnlyCollection<T>`, and non-generic `ICollection` Count evidence;
2. reject any first item beyond the admitted Count before null, duplicate, token, identity, or grouping work;
3. independently enforce the existing 10,000-item collection cap;
4. enumerate and perform the existing semantic accumulation;
5. reject under-yield when the enumerated item count differs from the admitted Count;
6. rebind supported Count evidence after traversal;
7. fail closed if rebound Count is changed, newly negative, newly conflicting, or no longer available after an admitted deterministic Count;
8. only then perform canonical sorting, identity validation, evidence grouping, and result publication.

For a pure streaming source with no supported deterministic Count metadata, the post-traversal rebind is a no-op and the independent 10,000-item cap remains the governing bound.

## Regression matrix

Deterministic Core smoke must prove:

- dimension Count drift fails before projection publication;
- provenance Count drift fails before projection publication;
- projection-set Count drift fails before identity sorting/publication;
- quantity-evidence Count drift fails before evidence sorting/grouping;
- exchange-result Count drift fails before result publication;
- newly negative Count evidence after traversal fails closed;
- newly conflicting supported Count evidence after traversal fails closed;
- stable two-phase counted inputs remain accepted and retain canonical ordering/grouping semantics;
- pure streaming inputs remain accepted under the unchanged streaming cap;
- the existing early-overrun and under-yield behavior remains intact.

`preflight-ifc-roundtrip-known-count-stability.py` statically guards the shared contract, all five production call sites, ordering relative to final under-yield checks and canonical publication, existing pre-item guards, existing size bounds, smoke registration, and this runbook.

## Runtime boundary

This is Core/export/trace data-integrity work. No licensed BricsCAD, private DWG, signing, packaging, or machine-only evidence is required. Hosted/source CI must not be represented as `LOCAL_PASS`; there is no licensed-runtime acceptance row in this lane.

## Merge boundary

The final candidate must be reconciled to exact current `main`, pass branch CI, then pass protected PR `preflight` and `core` on the current candidate. Only then may the same-task PR be merged under the repository's standing expected-head authorization, followed by exact protected-main verification.
