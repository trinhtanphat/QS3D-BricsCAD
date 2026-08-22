# QS3D Semantic Snapshot — read-only semantic diff

Updated: 2026-08-10 (UTC+7)

`ProjectInterchangeSnapshotDiff` compares two already-valid `QS3D.SemanticSnapshot` v1 snapshots without constructing or mutating a live `ProjectState`.

`CompareJson(left, right)` is validation-first because it reads both inputs through `ProjectInterchangeValidatedSnapshotReader`. Invalid JSON therefore fails before semantic comparison.

## Change model

The result contains deterministic read-only `InterchangeSnapshotChange` rows with:

- object kind: Manifest, Project, Zone, Floor, Family or Element;
- change kind: Added, Removed or Changed;
- stable semantic ID;
- sorted field names for Changed objects.

Changes are sorted by object kind, semantic ID and change kind. The diff fails closed above 120000 change rows rather than silently truncating a coordination result.

## Portable fields compared

Manifest comparison covers format/version and SI units. Project comparison covers project ID/name/schema version, drawing fingerprint and provenance timestamp.

Zone/Floor/Family comparison covers portable catalog identity/content. Family property keys are semantic case-insensitive while values remain exact.

Element comparison covers category, Family/Floor/Zone references, drawing fingerprint, provenance timestamp, source-reference scope, drawing-local source Handles, dependencies, portable properties and quantities.

Source Handles and dependency IDs are compared as case-insensitive sets because their list order is not semantic ownership/order. Properties and quantity maps are compared by case-insensitive key plus exact value. Quantity values are already bounded finite doubles because both inputs passed the strict validator/typed reader.

## Important provenance boundary

A change in `sourceHandles`, drawing fingerprint or timestamp is reported as a **portable provenance difference only**. The diff does not imply that either Handle owns the corresponding object in another DWG and does not authorize source rebinding.

Similarly, Added/Removed/Changed is descriptive. It is not a merge instruction and does not decide whether a future importer should add, delete, replace, rename, keep-target or regenerate anything.

## Read-only guarantees

The diff:

- does not create/replace `ProjectState`;
- does not write `.qsdb` or DWG;
- does not touch generated/native ownership;
- does not rebind drawing-local Handles;
- does not resolve import collision policy;
- does not mutate either typed input snapshot;
- returns defensive read-only change/field collections.

## Source checks

```text
python scripts/preflight-interchange-semantic-diff.py
```

`ProjectInterchangeSnapshotDiffSmoke` covers identical snapshots, Added/Removed/Changed classification, portable element-field differences, order-insensitive Handle/dependency comparison, validation-first JSON comparison and immutable result collections.

## Relationship to import work

The source-safe interchange pipeline is now:

```text
export -> validate -> immutable typed read -> semantic diff / target collision preview
```

This improves coordination/review without inventing a mutating import policy. `VALIDATE PASS`, typed-read success, no diff, or zero target collisions are still **not import permission**.

Current status: **REMOTE_DONE for deterministic read-only Semantic Snapshot v1 diff only**. Mutating import/round-trip, source rebinding and native ownership reconstruction remain separately reviewed/runtime-gated work.
