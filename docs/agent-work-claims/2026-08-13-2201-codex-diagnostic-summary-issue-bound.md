# Work claim — Diagnostic summary bounded issue enumeration

- Status: `ACTIVE`
- Agent: `Codex / GPT-5`
- Registered: `2026-08-13T22:01:00+07:00`
- Baseline main SHA: `2b1729b635ba0fd7ec23878dcb964651550122f3`
- Priority: evidence-driven remote-safe diagnostic export integrity

## Reason

`ProjectDiagnosticSummaryExporter.Build(..., IEnumerable<ModelHealthIssue>)` currently materializes caller-controlled health issues with an unbounded `ToList()`. A non-terminating or excessively large lazy sequence can therefore hang support-summary generation or consume memory without limit before validation. The current preflight smoke covers a lazy sequence that throws, but not bounded enumeration. This is a current-source instance of the roadmap's bounded lazy input/enumeration hardening class.

## Reserved scope

- `src/QS3D.Core/Diagnostics/ProjectDiagnosticSummaryExporter.cs`
- `tests/QS3D.Core.SmokeTests/ProjectDiagnosticSummaryPreflightSmoke.cs`
- `scripts/preflight-project-diagnostic-summary.py` only if the focused static gate needs the new bounded contract token
- this claim file

No other diagnostics, UI, BricsCAD adapter/runtime, release, GitHub Actions, P10, `#987`, `#1005`, or LOCAL-only surface is reserved.

## Intended contract

- Snapshot the issue enumerable once through an explicit maximum count and reject the first item beyond that maximum.
- Fail closed before directory creation or destination replacement when the input exceeds the cap.
- Preserve valid summary grouping/counts, null/undefined-severity rejection, malformed-Unicode rejection, throwing-enumerable behavior, privacy exclusions, and atomic publication.
- Add focused lazy-enumeration regression coverage proving the exact accepted count, one-item-over rejection, no partial publication, and no second enumeration.

## Completion condition

Implementation is complete only when the focused diagnostic smoke/static gate, full Core smoke, and aggregate source preflight pass on the implementation head, the implementation PR is merged normally, and this claim is updated to `COMPLETED` with exact merge and validation evidence.
