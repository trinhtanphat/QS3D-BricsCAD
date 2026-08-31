# IFC round-trip projection-set fail-fast semantic identity

Lane-Key: `issue-4561`

## Boundary

`IfcRoundTripProjectionSet.Create` accepts caller-controlled `IEnumerable<IfcRoundTripProjection>` input and publishes an immutable, canonically sorted projection set. Known Count evidence and the independent 10,000-entry ceiling remain traversal admission contracts.

A projection that has already been admitted by `MoveNext` and read through `Current` must have its set-level semantic identity validated before the consumer asks the caller-controlled source for another item. The decisive checks are:

1. reject a null projection;
2. reject duplicate `IfcGlobalId` using ordinal identity;
3. reject duplicate `Qs3dElementId` using the existing ordinal-ignore-case identity rule;
4. append only the validated projection to the snapshot;
5. after traversal, preserve exact known-Count cardinality and rebound Count stability;
6. sort only the fully validated snapshot before immutable publication.

## Defect reproduced from current main

Before `issue-4561`, ProjectionSet appended caller items first and performed null/duplicate identity validation only after the entire enumerable completed. If item N was already an invalid duplicate or null, a hostile/unreliable item N+1 could still execute `MoveNext`/`Current` behavior and mask the actual projection semantic error. This made invalid-input diagnostics and caller-side effects depend on unrelated tail enumeration.

## Deterministic acceptance

`IfcRoundTripProjectionFailFastIdentitySmoke` provides a streaming source with two decisive items and a throwing tail. Duplicate IFC identity, case-insensitive duplicate QS3D identity, and null projection must all terminate after exactly two successful `MoveNext` calls and two `Current` reads. The tail exception must never be observed.

`preflight-ifc-round-trip-projection-failfast-identity.py` pins source ordering and rejects regression to the historical post-traversal semantic validation scan.

Existing Count-overrun, under-yield/rebound stability, 10,000 ceiling, canonical sorting, projection equivalence, dimension/provenance and quantity-evidence contracts remain unchanged.

## Runtime boundary

This is deterministic Core-only behavior. Licensed BricsCAD V25/V26 runtime is `NOT_APPLICABLE`; no `LOCAL_PASS` is claimed or required.
