# Work claim — Measurement coverage CSV Unicode integrity

- Status: `ACTIVE`
- Agent: `audit-interchange-gap-next-20260815`
- Registered: `2026-08-15T09:45:00+07:00`
- Baseline main SHA: `1c5cc0a00ff8612102f63036d9d50f21cd0b1d75`
- Related issue: `#84`
- Priority: remote-safe measurement interchange correctness

## Confirmed defect

`MeasurementWorkItemCoverageCsvExporter` writes with `new UTF8Encoding(true)`, whose replacement fallback silently converts an unpaired UTF-16 high or low surrogate to U+FFFD. Mapping identities can reach this public export boundary with such malformed input, so the published portable CSV may contain a different identity from the in-memory coverage matrix instead of failing closed.

## Reserved scope

- `src/QS3D.Core/Export/MeasurementWorkItemCoverageCsvExporter.cs` — reject malformed UTF-16 before path, directory, or temporary-file side effects while retaining current BOM, CSV quoting, formula neutralization, and atomic replacement behavior.
- `tests/QS3D.Core.SmokeTests/MeasurementWorkItemCoverageCsvUnicodeIntegritySmoke.cs` — one self-registering deterministic regression for lone high/low surrogate rejection, failure atomicity, and ordinal preservation of valid supplementary Unicode.
- `scripts/preflight-measurement-work-item-coverage-csv-unicode.py` — one focused source guard.
- this claim file for exact coordination and closeout evidence.

## Explicit exclusions

- No coverage matrix, evaluator, mapping catalog, or domain-contract changes.
- No other CSV/XLSX exporters, semantic snapshot/import policy, IFC, BCF, native BricsCAD adapter/runtime, LOCAL probe/runner, private data, release/signing, or GitHub Actions work.
- No Unicode normalization or case-policy changes and no issue `#84` closure.

## Coordination evidence

At baseline `1c5cc0a00ff8612102f63036d9d50f21cd0b1d75`, issue `#84`, current source, recent main history, open PR file lists, relevant remote branches, and ACTIVE/BLOCKED claim text were inspected. Issue `#84` contains this agent's visible reservation, and no competing open PR or claim owns the exact exporter/test/gate surface.

## Validation plan

- focused CSV Unicode preflight plus relevant measurement/interchange gates;
- QS3D.Core and Core-smoke Release builds;
- full deterministic Core smoke;
- aggregate discovered source preflights;
- final current-main collision audit, exact diff review, normal PR merges, and exact-SHA readback.

## Completion condition

Malformed UTF-16 is rejected before any filesystem side effect, valid supplementary Unicode remains ordinally identical in the BOM-bearing UTF-8 CSV, existing CSV/formula/atomic publication contracts remain intact, all remote-safe validation passes, implementation and claim closeout reach `main`, and issue `#84` remains open for its broader native/runtime/format scope.
