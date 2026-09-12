# Cubicost concrete/formwork inventory aggregation

`CubicostConcreteFormworkWorkflow` is a host-neutral benchmark-parity contract in `QS3D.Core`. It does not depend on BricsCAD and remains usable by standalone drawing/model recognition, IFC ingestion, tests, services, and optional CAD adapters.

## Numeric publication contract

Concrete (`m3`) and formwork (`m2`) BOQ totals use compensated finite summation at the publication boundary. This preserves small quantities in high-dynamic-range groups such as `1e16 + 1 + 1`, rejects aggregate overflow instead of publishing `Infinity`/`NaN`, and canonicalizes zero to positive zero. Classification, unit, evidence/review semantics, source count, and public DTO shapes are unchanged.

The original Cubicost workflow and the reviewed downstream bridge share one internal aggregation primitive so Estimate/Tender/Procurement handoffs cannot drift from the legacy/public BOQ surface.

## Compatibility and architecture

The change is additive at the implementation level: no constructor, public method, enum, classification suffix, or unit contract changes. Existing finite ordinary totals remain numerically equivalent; only cases previously affected by floating-point loss or overflow become more precise or fail closed.

BricsCAD-specific adapters remain optional and must consume these Core contracts rather than reimplement quantity arithmetic. Licensed/native BricsCAD behavior is outside this managed-Core qualification; hosted CI proves only the host-neutral arithmetic and deterministic smoke contracts.

## Regression coverage

`QsCubicostLegacyInventoryAggregationSmoke` is auto-registered with a module initializer and covers compensated Concrete/Formwork totals, reverse-input determinism, source-count preservation, signed-zero canonicalization, and fail-closed aggregate overflow. It deliberately avoids the shared benchmark smoke registry so concurrently reserved benchmark lanes remain collision-free.
