# Work claim — semantic element table fatal runtime-health propagation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-semantic-element-table-fatal-health-20260812-0856`
- Registered: `2026-08-12T08:56:00+07:00`
- Baseline main SHA: `63bf8fb410901e6c79f22200a419abd616c0890f`
- Priority: owner-requested continue-all native runtime-health integrity hardening

## Confirmed defect

`GeneratedSemanticElementTableRuntimeHealthService` contains broad `catch (Exception)` / `catch { ... }` recovery inside the provider for semantic snapshot rendering, live Table cell reads, and CAD handle resolution. Those catches can absorb fatal runtime exceptions before the outer `GeneratedSolidRuntimeHealthService.AddProviderSafely(...)` boundary can apply its explicit fatal-exception policy. The runtime aggregator intentionally excludes `OutOfMemoryException`, `StackOverflowException`, and `AccessViolationException` from recoverable provider isolation, so this provider must not downgrade those failures into ordinary render/cell/missing diagnostics.

## Reserved scope

- Keep ordinary recoverable semantic-table runtime failures diagnostic and non-mutating.
- Do not catch `OutOfMemoryException`, `StackOverflowException`, or `AccessViolationException` inside this provider.
- Apply one consistent recoverable-exception predicate to the snapshot render catch, live cell-read catch, and `TryResolve(...)` CAD resolution catch.
- Preserve existing diagnostic codes, `OpenMode.ForRead`, issue detail limits, blocking metadata behavior, and provider ordering.
- Add one focused static regression preflight for fatal-exception propagation.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/Cad/GeneratedSemanticElementTableRuntimeHealthService.cs`
- `scripts/preflight-semantic-element-table-runtime-health-fatal.py`
- this claim file

## Excluded scope

- No native Table build/refresh/remove mutation changes.
- No changes to `SemanticElementTableBuilder` write lifecycle.
- No changes to Quantity/Template/Zone or other active agent lanes.
- No GitHub Actions dispatch, release publication, force push, or licensed BricsCAD V25/V26 runtime PASS claim.

## Validation plan

- Re-fetch current source and this claim after registration before editing.
- Add a provider-local `IsRecoverableDiagnosticFailure(Exception)` helper matching the native aggregator fatal exclusions.
- Add catch filters to snapshot rendering, cell reads, and handle resolution so recoverable failures keep existing diagnostics/false return while fatal failures propagate.
- Add a source preflight that rejects unfiltered broad catches in the provider and requires all three fatal exclusions.
- Re-fetch final source/preflight from current `main`, verify ancestry, then close this claim with exact SHAs.

## Completion condition

Completed only when current `main` no longer lets semantic element Table runtime health swallow the native runtime fatal-exception classes, recoverable diagnostics remain intact/read-only, focused regression source pins the contract, and this claim is `COMPLETED` with exact integration evidence.
