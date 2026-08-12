# Work claim — Grid annotation fatal runtime-health propagation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-grid-annotation-fatal-health-20260812-0858`
- Registered: `2026-08-12T08:58:00+07:00`
- Baseline main SHA: `4c5e9ea828acae8aef75654bd12c6f6b6d80ca29`
- Priority: owner-requested continue-all native runtime-health integrity hardening

## Confirmed defect

`GeneratedGridAnnotationRuntimeHealthService.InspectHandle(...)` uses bare `catch` blocks around `Database.GetObjectId(...)` and `Transaction.GetObject(...)`. The native runtime-health isolation contract established in `1332f53ac62b081c7c999c5f9f4721896c6e09e1` requires recoverable provider failures to become diagnostics while fatal runtime failures still bubble. These inner bare catches can absorb fatal exceptions before the outer runtime-health boundary enforces that policy, downgrading them to `GRID_ANNOTATION_CAD_MISSING`.

## Reserved scope

- Keep ordinary CAD resolution/read failures reported through the existing missing Grid annotation diagnostic.
- Do not catch `OutOfMemoryException`, `StackOverflowException`, or `AccessViolationException` inside Grid Annotation runtime health.
- Apply one provider-local recoverable-exception predicate to both CAD resolution/read catch points.
- Preserve current count/type/text/ownership diagnostics and `OpenMode.ForRead` behavior.
- Add one focused static regression preflight.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/Cad/GeneratedGridAnnotationRuntimeHealthService.cs`
- `scripts/preflight-grid-annotation-runtime-health-fatal.py`
- this claim file

## Excluded scope

- No Grid annotation creation/refresh/remove mutation changes.
- No Semantic Tag or semantic Table provider changes in this claim.
- No changes to unrelated active claims.
- No GitHub Actions, release publication, force push, or licensed BricsCAD V25/V26 runtime PASS claim.

## Validation plan

- Re-fetch current source after claim registration before editing.
- Add `IsRecoverableDiagnosticFailure(Exception)` matching the native aggregator fatal exclusions.
- Filter both CAD resolution/read catches so recoverable failures keep the existing missing diagnostic while fatal failures propagate.
- Add a focused source preflight requiring both filtered catches, all fatal exclusions and `OpenMode.ForRead`.
- Re-fetch final source/preflight from current `main`, verify ancestry, then close with exact SHAs.

## Completion condition

Completed only when current `main` no longer lets Grid Annotation runtime health swallow native fatal exception classes, ordinary missing diagnostics remain intact/read-only, regression source pins the contract, and this claim is `COMPLETED` with exact integration evidence.
