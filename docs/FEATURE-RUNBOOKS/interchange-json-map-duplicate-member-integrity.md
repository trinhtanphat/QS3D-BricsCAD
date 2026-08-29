# Interchange JSON map duplicate-member integrity

## Contract

The semantic-snapshot trust boundary must reject repeated JSON member names inside dictionary-shaped `family.properties`, `element.properties`, and `element.quantities` objects before `DataContractJsonSerializer` performs typed deserialization. Exact duplicate raw JSON names are structural ambiguity and report `JSON_DUPLICATE_MEMBER` at the containing map path.

Post-deserialization case-insensitive semantic-key checks remain authoritative for distinct raw member names that collide under QS3D property/quantity semantics.

## Deterministic acceptance

`ProjectInterchangeMapDuplicateMemberSmoke` covers duplicate family property, duplicate element property, duplicate element quantity, and a unique-map valid control. `scripts/preflight-interchange-json-map-duplicate-members.py` pins the structural inspection and its ordering before typed deserialization.

Licensed BricsCAD runtime is not applicable to this Core interchange integrity package.
