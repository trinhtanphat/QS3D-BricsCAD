# Work claim — semantic element table fatal runtime-health propagation

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-semantic-element-table-fatal-health-20260812-0856`
- Registered: `2026-08-12T08:56:00+07:00`
- Baseline main SHA: `63bf8fb410901e6c79f22200a419abd616c0890f`
- Priority: owner-requested continue-all native runtime-health integrity hardening

## Confirmed defect

`GeneratedSemanticElementTableRuntimeHealthService` contained broad `catch (Exception)` / `catch { ... }` recovery inside the provider for semantic snapshot rendering, live Table cell reads, and CAD handle resolution. Those catches could absorb fatal runtime exceptions before the outer `GeneratedSolidRuntimeHealthService.AddProviderSafely(...)` boundary applied its explicit fatal-exception policy. The runtime aggregator intentionally excludes `OutOfMemoryException`, `StackOverflowException`, and `AccessViolationException` from recoverable provider isolation, so this provider must not downgrade those failures into ordinary render/cell/missing diagnostics.

## Implemented scope

- Ordinary recoverable semantic-table runtime failures remain diagnostic and non-mutating.
- `OutOfMemoryException`, `StackOverflowException`, and `AccessViolationException` are no longer caught by the provider-local recovery paths.
- Snapshot rendering, live cell reads, and CAD handle resolution now use one consistent `IsRecoverableDiagnosticFailure(Exception)` predicate.
- Existing diagnostic codes, `OpenMode.ForRead`, issue detail limits and blocking metadata behavior remain intact.
- Added a focused static regression preflight for fatal-exception propagation.

## Implemented surfaces

- `src/QS3D.BricsCAD.V25/Cad/GeneratedSemanticElementTableRuntimeHealthService.cs`
- `scripts/preflight-semantic-element-table-runtime-health-fatal.py`
- this claim file

## Integration evidence

- Claim registration: `345fba95db904041cd4c225aae102ad17efc9880`
- Source fix: `826d384c8701e36a4b8dcffa706acabb04689ee9`
- Focused regression preflight: `3f91efec09bd4612e6091cdc561cde4b246c1c70`

## Validation performed

- Re-fetched current `main`, this claim and the exact V25 source after claim registration before editing; source blob `e8b98d9fab70e45e1fe6717b05daafb7bf6d79d1` still contained all three unfiltered broad recovery points.
- Re-fetched final source from current `main`; blob `ad3515714c221f87e8725e9a6263eb6d98548608` contains filtered recovery for snapshot rendering, live cell reads and CAD handle resolution, plus explicit exclusions for `OutOfMemoryException`, `StackOverflowException` and `AccessViolationException`.
- Re-fetched `scripts/preflight-semantic-element-table-runtime-health-fatal.py`; blob `20f9f9a27eb88296a62fbaec485f8194d57c4273` requires at least three filtered catches, all fatal exclusions, existing render/cell diagnostics and `OpenMode.ForRead`, and rejects the old unfiltered handle-resolution catch.
- V26 links the shared V25 adapter source through `QS3D.BricsCAD.V26.csproj`, so this source hardening is shared by the V26 build without a duplicate adapter implementation.

## Validation boundary

Remote source/static readback only. This session did not execute the preflight process, a full .NET build/test, GitHub Actions, or licensed BricsCAD V25/V26 runtime. No native runtime, private-DWG, installer, signing or release PASS is claimed.

## Excluded scope

- No native Table build/refresh/remove mutation changes.
- No changes to `SemanticElementTableBuilder` write lifecycle.
- No changes to Quantity/Template/Zone or other active agent lanes.
- No GitHub Actions dispatch, release publication or force push.

## Completion condition

Satisfied on the source/static contract: current `main` no longer lets semantic element Table runtime health swallow the native runtime fatal-exception classes, recoverable diagnostics remain intact/read-only, focused regression source pins the contract, and exact integration evidence is recorded above.
