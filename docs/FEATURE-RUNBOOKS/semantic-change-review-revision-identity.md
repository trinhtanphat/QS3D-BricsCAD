# Semantic change review revision identity integrity

## Scope

`SemanticChangeReviewBuilder.Build` accepts caller-constructible `RevisionSnapshot` values and publishes their IDs as `BeforeRevisionId` and `AfterRevisionId`. Those provenance identities must be canonical and compatible with the revision domain's XML-safe persistence/review boundary before any review object is returned.

## Fail-closed contract

Both revision IDs remain required and exact-trim canonical. Control characters are rejected, and `XmlConvert.VerifyXmlChars` rejects malformed UTF-16 surrogate sequences and other XML-invalid characters before semantic review materialization.

Valid supplementary-plane Unicode remains supported and is preserved exactly. The implementation must not normalize case, replace valid surrogate pairs, or rewrite identity text.

`RevisionService.Compare` remains the semantic element/project payload authority. This focused boundary does not duplicate element-delta validation; it closes the distinct snapshot-ID provenance gap in `SemanticChangeReviewBuilder`.

## Deterministic regression

The existing module-initialized `SemanticChangeReviewSmoke` is strengthened to prove:

- malformed before revision ID with a lone high surrogate is rejected;
- malformed after revision ID with a lone low surrogate is rejected;
- control-bearing revision IDs are rejected;
- valid supplementary-plane Unicode revision IDs are returned byte-semantically unchanged;
- historical deterministic ordering, portable-field filtering, and source-handle omission behavior remain covered.

The auto-discovered `scripts/preflight-semantic-change-review-revision-identity.py` pins the production admission and regression contract.

## Validation boundary

Runtime: NOT_APPLICABLE — deterministic Core revision-review identity integrity only. No licensed BricsCAD runtime evidence is required or claimed.
