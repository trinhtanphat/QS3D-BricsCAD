# Work claim — Quantity report structural read-only result

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-report-readonly-result-20260812-0813`
- Registered: `2026-08-12T08:13:00+07:00`
- Baseline main SHA: `9cc952dc2457c558dca2d81ffbc366a202b365e7`
- Priority: evidence-driven public Reporting result ownership during owner-requested `continue all`

## Confirmed defect

`QuantityReportBuilder.Group(IEnumerable<ElementInstance>)` declares `IReadOnlyList<QuantityReportRow>` but returns its mutable backing `List<QuantityReportRow>` directly. A caller can cast the result to a mutable collection and structurally add, remove or clear grouped rows after aggregation has completed.

## Reserved scope

- `src/QS3D.Core/Reporting/QuantityReportBuilder.cs` — return boundary only.
- `tests/QS3D.Core.SmokeTests/QuantityReportReadOnlyResultSmoke.cs` — focused CAD-independent regression.
- this claim file.

## Contract

Return a structural read-only wrapper for the completed grouped row list while preserving grouping order/key semantics, row objects, source-handle provenance, count arithmetic, quantity accumulation, duplicate-element rejection and every existing exception contract. No deep-immutability redesign of `QuantityReportRow` is included.

## Excluded scope

No ProjectQuantityReportBuilder selection logic, deduction rules, XLSX/export UI, CAD/native behavior, persistence, release/update or quantity arithmetic redesign.

## Validation plan

Build two ordinary report groups, preserve row ordering/counts, require the returned `ICollection<QuantityReportRow>` to be read-only, and prove structural `Add` throws `NotSupportedException`. Re-fetch current source before write; never force-push. No GitHub Actions dispatch or BricsCAD runtime qualification claim.
