# Live Workbook binding identity

`LiveWorkbookBinding.BindingId` and every entry in `DependsOnBindingIds` are opaque integration identities. They are now matched with ordinal, case-sensitive semantics throughout refresh normalization, duplicate detection, dependency graph construction, result lookup, cascade ordering, trace ordering, and deterministic output tie-breaking.

This prevents distinct external identifiers such as `LineA` and `linea` from collapsing into one binding or resolving a dependency to the wrong workbook/BOQ line. Exact-case dependency references are therefore required.

## Compatibility

This is an identity-correctness hardening change; public constructors, result DTOs, freshness states, stale/revision propagation, compensated source/dependency aggregation, and failure behavior are unchanged. Existing workbook/sheet/cell collision behavior and BIM/drawing source/revision comparison behavior remain case-insensitive for backward compatibility.

Consumers that previously relied on a dependency reference whose casing differed from its canonical `BindingId` must migrate that reference to the exact canonical casing. A casing mismatch now fails closed as a missing/cyclic dependency instead of silently aliasing another identifier.

## Integration example

Bindings `CostLineA` and `costlinea` may coexist as distinct identities. A binding declaring `DependsOnBindingIds = ["CostLineA"]` resolves only `CostLineA`; `"costlinea"` resolves only the lower-case identifier. Trace entries preserve that exact identity, keeping Power BI/ERP/CRM reconciliation and revision audits deterministic.