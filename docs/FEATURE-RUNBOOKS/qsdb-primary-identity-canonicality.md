# QSDB primary identity canonicality

## Scope

Issue #5232 / Lane-Key `issue-5232` closes a Core persistence read-boundary gap for primary persisted identities. It is intentionally separate from historical optional-reference work: active floor/zone and element family/floor/zone reference attributes retain their existing empty-or-canonical contract.

The affected primary identities are zone, floor, family, element and quantity-rule IDs, quantity-rule output identity, and element quantity names.

## Defect

`QsdbProjectStore.Load` historically used `Required(...)` for these attributes. `Required(...)` verifies nonblank input and then returns `value.Trim()`. A hand-edited, externally produced or corrupted QSDB could therefore persist `id=" E1 "` or `name=" AreaM2 "` and have the loader silently convert it to a different canonical identity before duplicate/reference/project validation ran.

Save-side validation is insufficient for an import boundary because not every persisted file was produced by the current in-process writer.

## Contract

`ProjectSchemaMigrator.MigrateToCurrent` now validates these raw XML primary identity/key attributes before `QsdbProjectStore` hydrates domain objects. Each covered attribute must exist, contain non-whitespace content, and already equal its trimmed representation ordinally. Malformed padding fails closed with `InvalidDataException`; no canonical alias is synthesized during hydration. The same validation is applied to already-current documents and to the migrated working tree before it replaces the caller document.

Free-text remains separate. Project/zone/floor/family names, property values, metadata values and audit detail/text are not reclassified as primary identity keys by this package. Optional relationship/reference attributes keep their existing validation path.

## Regression

`QsdbPrimaryIdentityCanonicalitySmoke` starts from a writer-produced canonical QSDB, tampers exactly one primary identity at a time with leading/trailing whitespace, and requires `Load` to fail for zone/floor/family/element/rule IDs, rule output and quantity name. A canonical control package must still round-trip unchanged.

`scripts/preflight-qsdb-primary-identity-canonicality.py` pins both current-schema and migration-path calls to the raw XML canonicality validator, every covered identity attribute, the fail-closed whitespace check, and deterministic smoke registration.

## Validation

Remote acceptance is deterministic Core validation: focused guard, registered smoke, aggregate discovered feature guards, Core Release build/smoke, protected exact-head `preflight + core`, latest-main collision reconciliation, expected-head PR merge and exact protected-main verification. Licensed BricsCAD runtime is not applicable and must not be claimed.
