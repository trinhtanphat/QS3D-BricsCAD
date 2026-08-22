# Work claim — Grid annotation fatal runtime-health propagation

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-grid-annotation-fatal-health-20260812-0858`
- Registered: `2026-08-12T08:58:00+07:00`
- Baseline main SHA: `4c5e9ea828acae8aef75654bd12c6f6b6d80ca29`
- Priority: owner-requested continue-all native runtime-health integrity hardening

## Confirmed defect

`GeneratedGridAnnotationRuntimeHealthService.InspectHandle(...)` used bare `catch` blocks around `Database.GetObjectId(...)` and `Transaction.GetObject(...)`. The native runtime-health isolation contract established in `1332f53ac62b081c7c999c5f9f4721896c6e09e1` requires recoverable provider failures to become diagnostics while fatal runtime failures still bubble. Those inner bare catches could absorb fatal exceptions before the outer runtime-health boundary enforced that policy, downgrading them to `GRID_ANNOTATION_CAD_MISSING`.

## Implemented scope

- Ordinary CAD resolution/read failures remain reported through the existing missing Grid annotation diagnostic.
- `OutOfMemoryException`, `StackOverflowException`, and `AccessViolationException` are no longer caught inside Grid Annotation runtime health.
- Both CAD resolution/read catch points now use one provider-local recoverable-exception predicate.
- Existing count/type/text/ownership diagnostics and `OpenMode.ForRead` behavior remain intact.
- Added a focused static regression preflight.

## Implemented surfaces

- `src/QS3D.BricsCAD.V25/Cad/GeneratedGridAnnotationRuntimeHealthService.cs`
- `scripts/preflight-grid-annotation-runtime-health-fatal.py`
- this claim file

## Integration evidence

- Claim registration: `30ae7ae3af6bd46a1c23b092cab53e95a29e533f`
- Source fix: `39d7d42a35c681dba6c15298d91197759c628a32`
- Focused regression preflight: `f85f26d0d0034244266a9106fae8ed78c2bfbfb1`

## Validation performed

- Re-fetched the exact Grid Annotation runtime health source after claim registration; blob `f4448cad2de52e40315a3bd1728356118b66009d` still contained both bare catches before editing.
- Re-fetched final source from current `main`; blob `8aa51d731ea3e18d897ab7f9d03574ca37ead9dc` contains two filtered `catch (Exception ex) when (IsRecoverableDiagnosticFailure(ex))` paths, preserves `GRID_ANNOTATION_CAD_MISSING`, and retains `OpenMode.ForRead`.
- Re-fetched `scripts/preflight-grid-annotation-runtime-health-fatal.py`; blob `c188f2d5f00465687660985402de3bfd0b871e00` requires both filtered catches, all three fatal exclusions, the existing missing diagnostic, and read-only object open mode.
- V26 links the shared V25 adapter source, so this hardening is shared by V26 source build without a duplicate provider implementation.

## Validation boundary

Remote source/static readback only. This session did not execute the preflight process, a full .NET build/test, GitHub Actions, or licensed BricsCAD V25/V26 runtime. No native runtime, private-DWG, installer, signing or release PASS is claimed.

## Excluded scope

- No Grid annotation creation/refresh/remove mutation changes.
- No Semantic Tag or semantic Table provider changes in this claim.
- No changes to unrelated active claims.
- No GitHub Actions, release publication or force push.

## Completion condition

Satisfied on the source/static contract: current `main` no longer lets Grid Annotation runtime health swallow native fatal exception classes, ordinary missing diagnostics remain intact/read-only, regression source pins the contract, and exact integration evidence is recorded above.
