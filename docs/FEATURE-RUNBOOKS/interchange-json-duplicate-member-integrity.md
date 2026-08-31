# Interchange JSON duplicate-member integrity

Lane-Key: `issue-4651`

## Boundary

`ProjectInterchangeJsonValidator` is a trust boundary for `.qs3d.json` semantic snapshots. A JSON object must have one unambiguous value for every supported contract member before any value is projected into `SnapshotContract` by `DataContractJsonSerializer`.

A repeated supported member is therefore malformed interchange input even when both values are textually equal. Accepting it would make semantic meaning dependent on serializer duplicate-key behavior and would permit ambiguous identity, version, provenance, collection, or element metadata to reach later validation.

The structural pass must run before semantic deserialization and must fail closed with `JSON_DUPLICATE_MEMBER` at the containing object path. This applies to the root object and every explicitly modeled nested object inspected by the v1 shape guard: `units`, `project`, `zones[]`, `floors[]`, `families[]`, and `elements[]`.

Existing unknown-member behavior remains independent: a unique unsupported member still produces `JSON_UNKNOWN_MEMBER`. Existing UTF-8/UTF-16, byte-size, collection, identity, dependency, property, quantity and provenance validation remains unchanged.

## Deterministic regression

`ProjectInterchangeDuplicateMemberSmoke` proves that duplicate supported members fail closed at root, nested project and array-object scope, while a unique unknown member retains the historical error code and a unique minimal valid snapshot remains accepted.

`python scripts/preflight-interchange-json-duplicate-members.py` pins both duplicate detection and the critical ordering `shape inspection -> DataContract deserialization` so a future refactor cannot move ambiguous structure back across the trust boundary.

## Validation

```text
python scripts/preflight-interchange-json-duplicate-members.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

Protected exact-head `preflight + core` are required before merge. Licensed BricsCAD/private-DWG runtime evidence is not applicable to this deterministic Core-only integrity lane.
