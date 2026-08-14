# Work claim — MAP-03C deterministic coverage CSV projection

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-map03c-coverage-csv-20260814`
- Registered UTC: `2026-08-14T01:17:00Z`
- Baseline main SHA: `fbd7a2bf9062347105fd8ad7f0652c8960b92b0b`
- Priority: `MAP-03 P1`

## Verified gap

MAP-03A provides deterministic per-element coverage rows and MAP-03B provides a compact category × measurement-item × mapped-work-item matrix with actionable affected-element ids. The live `src/QS3D.Core/Export` tree still has no coverage export projection, so the coverage truth cannot yet be emitted as a deterministic portable report without a consumer recreating rendering logic.

## Reserved scope

- new `src/QS3D.Core/Export/MeasurementWorkItemCoverageCsvExporter.cs`
- one focused self-registering Core smoke regression
- this claim file

## Bounded implementation

- consume `MeasurementWorkItemCoverageMatrix` only; do not evaluate project readiness or mapping inside the exporter;
- emit deterministic RFC4180-compatible UTF-8 text with a fixed invariant header/order;
- include category, measurement item, mapping/classification/work-item identity, readiness, issue set, finding count, affected-element count and affected element ids;
- preserve explicit missing/unmapped identities as empty CSV fields rather than inventing sentinel business ids;
- quote/escape commas, quotes and line breaks deterministically;
- reject null input and keep the export projection side-effect free;
- do not modify MAP-03A/B semantics, mapping resolver/evaluator, persistence, rates/cost, UI/V25/native surfaces, or BricsCAD integration.

## Validation policy

No GitHub Actions will be dispatched. Managed/native PASS will only be reported if actually executed. No force-push.
