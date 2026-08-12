# Work claim — Quantity report semantic selection freshness

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-report-semantic-selection-freshness`
- Registered: `2026-08-12T11:42:00+07:00`
- Baseline main SHA: `85e6410783fb2fc69de1207b57b5458b543c416e`
- Priority: P2 — fail closed when caller-controlled lazy report selection changes the project semantic version during enumeration.

## Confirmed defect

`ProjectQuantityReportBuilder.ResolveSelection(...)` already protects structural freshness by re-checking selected element instances after caller-controlled `IEnumerable<string>` enumeration. It does not capture or re-check `ProjectState.ChangeVersion`. A lazy selection can call `project.Touch()` while yielding the same selected element instances (or even yield no ids), after which report construction continues across a project semantic-version boundary.

## Reserved scope

- `src/QS3D.Core/Reporting/ProjectQuantityReportBuilder.cs`, limited to semantic-version freshness around selection enumeration
- focused Core smoke regression + ModuleInitializer registration under `tests/QS3D.Core.SmokeTests/`
- focused static preflight under `scripts/`
- `docs/plans/2026-08-12-quantity-report-semantic-selection-freshness.md`
- this claim file

## Intended contract

- Capture `project.ChangeVersion` immediately before caller-controlled selection enumeration.
- If `ChangeVersion` differs after enumeration, fail before report aggregation.
- Preserve the existing structural selected-instance freshness checks for direct collection remove/replace mutations that may not increment `ChangeVersion`.
- A mutating empty lazy selection must also fail closed.
- Stable lazy selections retain existing Group/Detail behavior.

## Excluded scope

- Existing structural selection-freshness lane completed by `53b99cd5b89ef722bc7d51215801a4ee190a456c`.
- Quantity formulas/grouping semantics unrelated to selection freshness.
- UI/export/runtime integration.
- GitHub Actions or licensed BricsCAD runtime qualification.
