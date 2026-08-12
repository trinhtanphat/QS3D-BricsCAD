# Work claim — semantic tag fatal runtime-health propagation

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-semantic-tag-fatal-health-20260812-0857`
- Registered: `2026-08-12T08:57:00+07:00`
- Baseline main SHA: `ecf86e34009ffd962ed75752db02fa85af8926af`
- Priority: owner-requested continue-all native runtime-health integrity hardening

## Confirmed defect

`GeneratedSemanticTagRuntimeHealthService` used bare `catch` blocks around `Database.GetObjectId(...)` and `Transaction.GetObject(...)`. The native runtime-health isolation contract established in `1332f53ac62b081c7c999c5f9f4721896c6e09e1` requires recoverable provider failures to become diagnostics while fatal runtime failures still bubble. Those inner bare catches could absorb fatal exceptions before the outer `GeneratedSolidRuntimeHealthService.AddProviderSafely(...)` boundary enforced that policy, downgrading them to `SEMANTIC_TAG_MTEXT_MISSING`.

## Implemented scope

- Ordinary handle-resolution/read failures remain reported through the existing missing-tag diagnostic.
- `OutOfMemoryException`, `StackOverflowException`, and `AccessViolationException` are no longer caught by Semantic Tag runtime health.
- Both CAD resolution/read catch points now use one provider-local recoverable-exception predicate.
- Existing ownership/content/height/placement diagnostics, `OpenMode.ForRead`, and read-only behavior remain intact.
- Added a focused static regression preflight.

## Implemented surfaces

- `src/QS3D.BricsCAD.V25/Cad/GeneratedSemanticTagRuntimeHealthService.cs`
- `scripts/preflight-semantic-tag-runtime-health-fatal.py`
- this claim file

## Integration evidence

- Claim registration: `7c39b3282ac170e492764cd330867825c881b165`
- Source fix: `074ebe1a79b7ead9e03aac777e013f6fcfb4b8a2`
- Focused regression preflight: `351478288c27f025a62afdf04960b49e2ee3c129`

## Validation performed

- Re-fetched the exact Semantic Tag runtime health source after claim registration; blob `5dc9d42747f75aa5c18bb9165137f71de08d834c` still contained both bare catches before editing.
- Re-fetched final source from current `main`; blob `64b848c5ebb882070e7e9cc26fc7333ca913b428` contains two filtered `catch (Exception ex) when (IsRecoverableDiagnosticFailure(ex))` paths, preserves `SEMANTIC_TAG_MTEXT_MISSING`, and retains `OpenMode.ForRead`.
- Re-fetched `scripts/preflight-semantic-tag-runtime-health-fatal.py`; blob `463307d6b627d95e9ed7baf98d554799649ff21a` requires both filtered catches, all three fatal exclusions, the existing missing diagnostic, and the read-only object open mode.
- V26 links the shared V25 adapter source, so this hardening is shared by V26 source build without a duplicate provider implementation.

## Validation boundary

Remote source/static readback only. This session did not execute the preflight process, a full .NET build/test, GitHub Actions, or licensed BricsCAD V25/V26 runtime. No native runtime, private-DWG, installer, signing or release PASS is claimed.

## Excluded scope

- No semantic tag creation/refresh/remove mutation changes.
- No Grid Annotation provider changes in this claim.
- No changes to unrelated active claims.
- No GitHub Actions, release publication or force push.

## Completion condition

Satisfied on the source/static contract: current `main` no longer lets Semantic Tag runtime health swallow native fatal exception classes, ordinary missing-tag diagnostics remain intact/read-only, regression source pins the contract, and exact integration evidence is recorded above.
