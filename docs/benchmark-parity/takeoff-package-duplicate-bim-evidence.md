# Takeoff Package duplicate BIM quantity evidence

## Scope

This parity guard applies at the Autodesk-style Takeoff Package admission boundary before BIM quantities enter the shared Classification → Quantity → Formula → Inventory → Estimate workflow.

## Identity rule

A BIM element may legitimately expose multiple quantity records, so IFC GUID alone is not a duplicate key. Package admission now treats `(Guid, QuantityName, Unit)` as the BIM quantity evidence identity using case-insensitive comparison.

Repeated records with the same identity produce `PKG.DUPLICATE_BIM_QUANTITY` at Error severity. The package becomes `Blocked`, and no inventory or estimate is published. This prevents duplicate/deserialized BIM evidence from being counted twice downstream.

Distinct quantity names on the same IFC GUID remain valid. The same quantity name expressed in a distinct unit also remains a distinct evidence identity; package-level conversion or normalization can be layered separately without silently discarding source evidence.

## Compatibility

No public DTO shape changes. Existing valid `IfcQtoItem` inputs continue through the same workflow, and package source cards remain element-oriented: multiple valid quantities from one IFC element still create one `Bim3D` source entry.

Consumers that previously relied on repeating the same `(Guid, QuantityName, Unit)` row must remove the duplicate before package build. This is intentionally fail-closed because preserving both rows would inflate measured quantity, formula output, and estimated cost.

## Validation

`TakeoffPackageDuplicateBimEvidenceSmoke` covers both the blocked duplicate case and the compatibility case where one IFC element carries multiple distinct quantity identities. The smoke is auto-discovered with a module initializer and does not require shared benchmark registry edits.
