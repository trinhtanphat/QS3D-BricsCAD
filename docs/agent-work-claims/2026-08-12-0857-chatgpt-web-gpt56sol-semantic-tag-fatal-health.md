# Work claim — semantic tag fatal runtime-health propagation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-semantic-tag-fatal-health-20260812-0857`
- Registered: `2026-08-12T08:57:00+07:00`
- Baseline main SHA: `ecf86e34009ffd962ed75752db02fa85af8926af`
- Priority: owner-requested continue-all native runtime-health integrity hardening

## Confirmed defect

`GeneratedSemanticTagRuntimeHealthService` uses bare `catch` blocks around `Database.GetObjectId(...)` and `Transaction.GetObject(...)`. The native runtime-health isolation contract established in `1332f53ac62b081c7c999c5f9f4721896c6e09e1` requires recoverable provider failures to become diagnostics while fatal runtime failures still bubble. These inner bare catches can absorb fatal exceptions before the outer `GeneratedSolidRuntimeHealthService.AddProviderSafely(...)` boundary can enforce that policy, downgrading them to `SEMANTIC_TAG_MTEXT_MISSING`.

## Reserved scope

- Keep ordinary handle-resolution/read failures reported through the existing missing-tag diagnostic.
- Do not catch `OutOfMemoryException`, `StackOverflowException`, or `AccessViolationException` inside Semantic Tag runtime health.
- Apply a provider-local recoverable-exception predicate to both CAD resolution/read catch points.
- Preserve current ownership/content/height/placement diagnostics, `OpenMode.ForRead`, and read-only behavior.
- Add one focused static regression preflight.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/Cad/GeneratedSemanticTagRuntimeHealthService.cs`
- `scripts/preflight-semantic-tag-runtime-health-fatal.py`
- this claim file

## Excluded scope

- No semantic tag creation/refresh/remove mutation changes.
- No Grid Annotation provider changes in this claim; that sibling will remain separate.
- No changes to unrelated active claims.
- No GitHub Actions, release publication, force push, or licensed BricsCAD V25/V26 runtime PASS claim.

## Validation plan

- Re-fetch current source after claim registration before editing.
- Add `IsRecoverableDiagnosticFailure(Exception)` matching the native aggregator fatal exclusions.
- Filter both CAD resolution/read catches so recoverable failures keep the existing missing diagnostic while fatal failures propagate.
- Add a focused source preflight requiring both filtered catches, all fatal exclusions and `OpenMode.ForRead`.
- Re-fetch final source/preflight from current `main`, verify ancestry, then close with exact SHAs.

## Completion condition

Completed only when current `main` no longer lets Semantic Tag runtime health swallow native fatal exception classes, ordinary missing-tag diagnostics remain intact/read-only, regression source pins the contract, and this claim is `COMPLETED` with exact integration evidence.
