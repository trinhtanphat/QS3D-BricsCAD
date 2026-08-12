# Work claim — quantity report selection structural freshness

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-report-selection-structural-freshness-20260812-1032`
- Registered: `2026-08-12T10:32:00+07:00`
- Baseline main SHA: `49a4e0345e2c82e259d28bed1b53580a25e527fc`
- Priority: evidence-driven remote-safe reporting consistency

## Confirmed defect

`ProjectQuantityReportBuilder.Group/Detail(project, elementIds)` validates project identity before enumerating the caller-provided `IEnumerable<string>`. `ResolveSelection(...)` then validates each yielded id with `project.FindElement(id)`, but a lazy enumerable can directly remove or replace that exact `ProjectElement` in the public `project.Elements` list between yields without changing `ProjectState.ChangeVersion`. After enumeration completes, `Build(...)` iterates the current list and can silently omit a selected element that had already passed validation, or report a replacement instance under the same id.

## Reserved scope

- Fail closed when a selected semantic element is structurally removed/rebound while the lazy selection enumerable is being consumed.
- Revalidate current project identity after selection enumeration so duplicates/null/malformed references introduced by the enumerable cannot flow into reporting.
- Preserve normal semantic-id selection, case-insensitive id matching, canonical/duplicate/missing-id validation, report grouping/detail calculations and output ordering.

## Expected surfaces

- `src/QS3D.Core/Reporting/ProjectQuantityReportBuilder.cs`
- one focused `QS3D.Core.SmokeTests` regression file with isolated `ModuleInitializer`
- this claim file

## Excluded scope

- `ReportingProjectIdentityGuard.cs` shared policy, report row mutability/readonly semantics, quantity math, source-handle resolution, Room Finish lifecycle semantics, native/UI/runtime.
- No GitHub Actions or LOCAL_ONLY qualification.

## Validation plan

- lazy selection that removes an already-yielded selected element fails closed instead of returning a partial Group report;
- lazy selection that replaces an already-yielded selected element with a new instance under the same id fails closed instead of reporting the rebound object;
- stable lazy selection still produces the same Group/Detail rows;
- current duplicate/canonical/missing selection behavior remains unchanged;
- moving-main target overlap is rechecked before integration.

## Completion condition

Focused source/regression are integrated on current `main`, remote source/test are re-read, and this claim is closed `COMPLETED` with exact integration evidence.
