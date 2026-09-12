# Integration API revision timestamp determinism

`QsApiRevisionDto.CreatedUtc` is part of the v1 integration contract consumed by REST clients such as Power BI, ERP and CRM integrations.

## Contract

- `DateTimeKind.Utc` values are preserved exactly.
- `DateTimeKind.Local` values are converted to UTC using the explicit local offset semantics supplied by .NET.
- `DateTimeKind.Unspecified` values are treated as UTC wall-clock values by applying `DateTimeKind.Utc` without changing ticks.

The last rule is deliberate. Calling `ToUniversalTime()` on an `Unspecified` value interprets that value through the machine's local timezone. The same revision timestamp would therefore serialize differently on agents in different regions. API v1 now removes that host dependency while keeping the existing constructor and DTO fields unchanged.

## Compatibility

No route, scope, DTO property name, API version, ETag format or authorization rule changes. Callers that already pass UTC timestamps observe no behavior change. Explicit local timestamps retain normal .NET local-to-UTC conversion. Only ambiguous `Unspecified` timestamps become deterministic across hosts.

Regression coverage lives in `BenchmarkParitySuiteSmoke.IntegrationApiNormalizesRevisionTimeDeterministically`.