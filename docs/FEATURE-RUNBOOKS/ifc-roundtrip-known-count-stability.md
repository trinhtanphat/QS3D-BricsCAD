# IFC round-trip known Count stability

Status: `SOURCE_READY / PENDING_BRANCH_CI`

Historical foundation: issue-4373. Current extension: `issue-4679`.

## Scope

This package hardens deterministic collection-count evidence across the IFC projection collection boundaries that canonicalize export/trace state. The issue-4373 source rebound Count only after traversal. Issue-4679 additionally rejects transient Count drift while traversal is active, before affected `Current` values are observed.

The five established IFC boundaries remain:

1. `IfcRoundTripProjection.CanonicalizeDimensions`;
2. `IfcRoundTripProjection.CanonicalizeProvenance`;
3. `IfcRoundTripProjectionSet.Create`;
4. `IfcRoundTripQuantityEvidenceSet.Create`;
5. `IfcRoundTripExchangeResultSet.Create`.

Issue-4679 changes the first three projection-owned boundaries. Quantity-evidence and exchange-result behavior remains under the existing post-traversal stability contract and historical guards.

## Required projection ordering

For a projection source exposing deterministic Count metadata, the acceptance sequence is:

1. observe and validate supported `ICollection<T>`, `IReadOnlyCollection<T>`, and non-generic `ICollection` Count evidence;
2. rebind Count immediately before each `MoveNext`;
3. after a successful `MoveNext`, rebind Count again before any `Current` read;
4. reject transient value drift, negative Count, or conflicting Count evidence even if later reads would restore the admitted Count;
5. reject any first item beyond the admitted Count before null, duplicate, token, identity, or grouping work;
6. independently enforce the existing 10,000-item collection cap;
7. perform existing semantic accumulation;
8. reject under-yield when enumerated item count differs from the admitted Count;
9. rebind supported Count evidence after traversal;
10. only then perform canonical sorting and publication.

For a pure streaming source with no supported deterministic Count metadata, traversal rebinds are no-ops and the independent 10,000-item cap remains governing.

## Regression matrix

Deterministic Core smoke must prove:

- transient dimension Count growth/shrink fails before the next dimension `Current` read;
- transient provenance Count drift fails before token normalization;
- transient projection-set Count drift fails before projection identity reads;
- transient negative/conflicting Count evidence fails closed during traversal;
- restored-after-drift Count cannot evade detection;
- stable counted inputs remain accepted and retain canonical ordering;
- pure streaming inputs remain accepted;
- historical post-traversal drift, early-overrun, under-yield, duplicate identity and 10,000-item bounds remain intact.

`preflight-ifc-roundtrip-known-count-stability.py` statically guards the shared during/after traversal contract, the three projection-owned call sites, ordering before `Current`, historical post-traversal checks, existing pre-item guards and existing size bounds.

## Runtime boundary

This is Core/export/trace data-integrity work. No licensed BricsCAD, private DWG, signing, packaging, or machine-only evidence is required. Hosted/source CI must not be represented as `LOCAL_PASS`; there is no licensed-runtime acceptance row in this lane.

## Merge boundary

The final candidate must be reconciled to exact current `main`, pass branch CI, then pass protected PR `preflight` and `core` on the current candidate. Only then may the same-task PR be merged under expected-head authorization, followed by exact protected-main verification.
