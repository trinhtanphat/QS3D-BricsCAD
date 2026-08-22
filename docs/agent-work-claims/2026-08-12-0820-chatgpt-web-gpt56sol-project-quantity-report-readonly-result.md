# Work claim — Project quantity report structural read-only result

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-project-quantity-report-readonly-result-20260812-0820`
- Registered: `2026-08-12T08:20:00+07:00`
- Baseline main SHA: `1d99fe7b9b8b8a753054591b11439256ed7c3ad9`
- Priority: evidence-driven Reporting result ownership during owner-requested `continue all`

## Confirmed defect

All public `ProjectQuantityReportBuilder.Group(...)` and `Detail(...)` paths share private `Build(...)`, whose return type is `IReadOnlyList<QuantityReportRow>` but whose completed result is returned as `order.Select(...).ToList()` directly. Callers can cast the result to a mutable collection and structurally add, remove or clear rows after aggregation has completed.

## Reserved scope

- `src/QS3D.Core/Reporting/ProjectQuantityReportBuilder.cs` — final return boundary only.
- `tests/QS3D.Core.SmokeTests/ProjectQuantityReportReadOnlyResultSmoke.cs` — focused CAD-independent regression.
- this claim file.

## Contract

Return a structural read-only wrapper for both Group and Detail result lists while preserving grouping/detail keys, selection canonicality, ordering, quantities, mass/density handling, source-handle resolution, identity validation and all existing exception behavior. No deep-immutability redesign of `QuantityReportRow`.

## Coordination

The recent duplicate-selection canonicality source/regression remains authoritative. This lane does not edit `ResolveSelection(...)`, selection enumeration, source-handle traversal or any local V25/Core-gate reserved file.

## Excluded scope

No quantity rules/settings/preset behavior, deduction logic, Locate/UI, XLSX/export, CAD/native behavior, persistence or release/update changes.

## Validation plan

Build a minimal project and exercise both Group and Detail, preserving expected row/count/length behavior. Require both returned values to expose read-only `ICollection<QuantityReportRow>` boundaries and prove structural `Add` throws `NotSupportedException`. Re-fetch current source before write; never force-push. No GitHub Actions dispatch or BricsCAD runtime qualification claim.
