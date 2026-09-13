# Solibri QA Gate 2.0 — IFC metadata evidence validation

## Scope

QA Gate 2.0 requires usable IFC entity and quantity-unit metadata before quantity workflows proceed. `QA2.MISSING_IFC_ENTITY` and `QA2.MISSING_QUANTITY_UNIT` remain Error-level findings in `SolibriQuantityStrict()` and therefore block Takeoff, BOQ, and Estimate at the default Error threshold.

## Evidence contract

Adapters must not use explicit negative placeholders as successful IFC metadata. After trimming and case-insensitive normalization, `0`, `false`, `missing`, `none`, `n/a`, `na`, `null`, `absent`, and `no` are treated as missing evidence. Real IFC entity names such as `IfcWall` / `IfcBeam` and real units such as `m`, `m2`, `m3`, and `kg` remain valid.

The rule IDs and severity/profile contracts are unchanged. These completeness findings remain eligible for the existing audited waiver mechanism; structural identity rules keep their separate non-waivable Critical behavior.

## Compatibility and migration

Existing integrations that already emit canonical IFC entity names and quantity units require no change. Integrations that previously serialized sentinel text (`false`, `0`, `missing`, `none`, `n/a`, `null`, `absent`, or `no`) must migrate to either real metadata or omit the field so QA2 reports the expected completeness finding. Do not replace missing source data with fabricated entity or unit values merely to clear the gate.

This is intentionally fail-closed for quantity safety and keeps the QS Intelligence flow consistent with the hardened Pset and IFC relationship evidence checks.
