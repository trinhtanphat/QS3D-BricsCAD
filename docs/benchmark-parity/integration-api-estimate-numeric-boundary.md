# Integration API estimate numeric boundary

`QsApiEstimateLineDto` is part of the versioned Integration API v1 contract used by Power BI, ERP, CRM and other JSON-facing hosts.

Both input fields (`Quantity` and `UnitRate`) must be finite. The derived `Amount` is now also materialized and validated as finite when the DTO is constructed. This closes the IEEE-754 overflow case where two individually finite operands such as `double.MaxValue` and `2d` multiply to positive infinity.

Fail-closed admission is intentional: a non-finite derived amount is rejected before the DTO can enter `QsApiProjectSnapshot.Estimates` or be serialized by a transport host. This keeps estimate payloads valid for strict JSON serializers and prevents downstream integrations from receiving `NaN`/`Infinity` values.

Finite calculations are backward compatible. `LineId`, `Quantity`, `UnitRate`, `Amount`, API version `1.0`, routes, scopes, ETags and response shapes are unchanged. The only behavior change is that estimate lines whose derived amount is non-finite are no longer admitted.

Regression coverage lives in `BenchmarkParitySuiteSmoke.IntegrationApiRejectsNonFiniteEstimateAmount`, which proves a normal finite line still publishes the expected amount and an overflowing line is rejected before publication.
