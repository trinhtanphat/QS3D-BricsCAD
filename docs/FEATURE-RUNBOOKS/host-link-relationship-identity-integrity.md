# Host-link relationship identity integrity

Lane-Key: `issue-5226`

## Defect

`HostLinkService` consumes relationship identity from caller ids, persisted `HostWallId`, and mutable `ProjectElement.DependsOn` collections. Before this carrier, the service only enforced surrounding-whitespace canonicality on these paths. A control-bearing, malformed UTF-16, or XML-invalid dependency that did not match the requested host could survive `LinkOpening` while a new canonical `HostWallId`/dependency was published.

## Contract

Before semantic mutation or audit publication:

- caller opening/host ids remain exact semantic identifiers without surrounding whitespace and must contain XML-safe text;
- persisted nonblank `HostWallId` must be canonical and XML-safe;
- every existing opening dependency must be nonblank, canonical, control-free, and valid XML/UTF-16 text;
- host dependency-graph traversal applies the same text-integrity check before resolving dependency identity;
- valid Unicode, case-insensitive semantic lookup, and canonical persisted element ids remain supported.

Malformed UTF-16 and XML-invalid relationship provenance fails closed. Control characters fail with an explicit repair diagnostic. The service does not silently sanitize or delete hostile provenance because that would hide corrupted semantic state.

## Deterministic regression

`HostLinkRelationshipIdentityIntegritySmoke` covers:

1. a malformed non-matching opening dependency that previously survived successful link publication;
2. control-bearing opening dependency rejection;
3. XML-invalid host graph dependency rejection;
4. malformed persisted `HostWallId` rejection during unlink;
5. hostile caller identity rejection plus valid Unicode/case-insensitive lookup preservation.

All rejection cases assert failure before the tested host relationship mutation.

## Validation

Run the focused source guard:

`python scripts/preflight-host-link-relationship-identity-integrity.py`

Then run the normal deterministic Core smoke/build path. Merge eligibility remains bound to fresh exact-head protected `preflight + core` SUCCESS, current-main freshness, collision cleanliness and mergeability.

Runtime: NOT_APPLICABLE. No licensed BricsCAD `LOCAL_PASS` claim is part of this source-only contract.
