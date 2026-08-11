# Work claim — BOM release diagnostics exception redaction

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-bom-diagnostics-redaction-20260812-0015`
- Registered: `2026-08-12T00:15:00+07:00`
- Baseline main SHA: `b78b069f3df92de6a7a740ac468edecc94216ae2`
- Priority: P1 — privacy-safe release diagnostics must not expose arbitrary exception detail from semantic/provenance/report failures.

## Reserved scope

Harden `BomReleaseGuardService` so its public `ModelHealthIssue.Message` values never concatenate caught `Exception.Message` text. Preserve issue codes, severity, element targeting, fail-closed behavior, report/traceability checks and all non-exception diagnostic detail.

## Expected surfaces

- `src/QS3D.Core/Diagnostics/BomReleaseGuardService.cs`
- one focused deterministic Core/static regression surface, preferably an existing BOM smoke/preflight when suitable, otherwise a new narrow preflight under `scripts/`
- this claim file for close-out

## Excluded scope

- Quantity Settings diagnostic commands/exporters (owned by the separate active quantity diagnostics redaction claim)
- exception wording produced by lower-level stores/services for interactive/local troubleshooting
- generated-health ownership logic, quantity calculation rules, reporting grouping semantics, BricsCAD runtime, installer/updater, release publication and GitHub Actions

## Validation plan

- Pin stable generic messages for `BOM_EXCLUSION_FAILED`, `BOM_TRACEABILITY_FAILED` and `BOM_REPORT_FAILED`.
- Guard that `BomReleaseGuardService` does not append `ex.Message`, exception `ToString()`, stack traces or filesystem paths to release-facing issues.
- Preserve the existing error codes/severities and fail-closed continuation behavior.
- Re-fetch current `main` before integration and reapply onto the latest head without overwriting concurrent work.
- No GitHub Actions dispatch; no BricsCAD V25 runtime PASS claimed.

## Coordination

The active Quantity Settings error-redaction claim owns only the two BricsCAD Quantity Settings diagnostic command files and explicitly excludes exception swallowing elsewhere. Recent BOM handle-case work is `COMPLETED`. This reservation owns only the Core BOM release guard messages above.

## Completion condition

BOM release-health exceptions remain fail-closed but no longer echo arbitrary exception details through `ModelHealthIssue.Message`, focused regression coverage is committed to current `main`, and this claim is marked `COMPLETED` with the implementation SHA and validation evidence.
