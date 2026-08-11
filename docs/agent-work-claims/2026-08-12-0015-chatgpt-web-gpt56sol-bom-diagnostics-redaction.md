# Work claim — BOM release diagnostics exception redaction

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-bom-diagnostics-redaction-20260812-0015`
- Registered: `2026-08-12T00:15:00+07:00`
- Baseline main SHA: `b78b069f3df92de6a7a740ac468edecc94216ae2`
- Priority: P1 — privacy-safe release diagnostics must not expose arbitrary exception detail from semantic/provenance/report failures.

## Reserved scope

Harden `BomReleaseGuardService` so its public `ModelHealthIssue.Message` values never concatenate caught `Exception.Message` text. Preserve issue codes, severity, element targeting, fail-closed behavior, report/traceability checks and all non-exception diagnostic detail.

## Expected surfaces

- `src/QS3D.Core/Diagnostics/BomReleaseGuardService.cs`
- `tests/QS3D.Core.SmokeTests/BomReleaseGuardSmoke.cs`
- this claim file for close-out

## Excluded scope

- Quantity Settings diagnostic commands/exporters (owned by the separate quantity diagnostics redaction lane)
- exception wording produced by lower-level stores/services for interactive/local troubleshooting
- generated-health ownership logic, quantity calculation rules, reporting grouping semantics, BricsCAD runtime, installer/updater, release publication and GitHub Actions

## Validation delivered

- Merge commit: `3be032f3a6f9567eb7a2de1098fd27165bc2a797` via PR #570.
- `BOM_EXCLUSION_FAILED`, `BOM_TRACEABILITY_FAILED` and `BOM_REPORT_FAILED` keep their existing Error severity/fail-closed paths but now emit stable generic messages instead of concatenating caught exception detail.
- Existing BOM smoke coverage now pins exact redacted messages for exclusion/report failures and adds a deterministic duplicate-element traceability failure that pins the traceability message.
- PR #570 was verified mergeable with exactly two changed product/test files before merge.
- No GitHub Actions workflow was dispatched by this lane.
- No licensed BricsCAD V25 runtime PASS is claimed. This remote session could not execute a local checkout/build because its shell environment could not resolve GitHub; validation here is source/diff plus committed deterministic smoke coverage, not an executed local runtime qualification.

## Coordination

The Quantity Settings error-redaction work remains separate. This completed reservation changed only the Core BOM release guard and its existing smoke test; no neighboring active lane was overwritten.

## Completion condition

Satisfied: BOM release-health exceptions remain fail-closed, arbitrary caught exception details are no longer echoed through the three BOM release-facing `ModelHealthIssue.Message` paths, focused regression coverage is on `main`, and this claim records the exact merge evidence.
