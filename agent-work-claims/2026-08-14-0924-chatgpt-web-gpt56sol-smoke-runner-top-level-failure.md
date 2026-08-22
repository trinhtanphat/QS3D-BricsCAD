# Work claim — Smoke runner top-level failure containment

- Status: `SOURCE_FIXED / PENDING_FRESH_SMOKE`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-14T09:24:00+07:00`
- Baseline main SHA: `0aea8c2a986bd443eb73b7f456082a540489cfa9`
- Evidence source: Google Sheet `TEST QS3D`, Ver 6 testcase, screenshot showing `QS3D.Core.SmokeTests.exe - Application Error` with CLR exception code `0xe0434352`.
- Claim commit: `e98c30fb79abe41e0f9df6b5cd1d175152453675`
- Scope amendment: `b3d14f8114892de4f8f4d6fdee18aca8f5650ebe`
- Source/regression fix: `61df0a3d28757e39dfc92fbf4f1cae6e4b968d48`

## Reserved scope

Prevent the Core smoke executable from terminating through an uncaught registered-smoke exception and surfacing as a Windows application-error popup. Preserve a non-zero process exit for failed smoke validation while printing the actual failing exception to the console.

## Implemented

- `QS3D.Core.SmokeTests.csproj` now selects `QS3D.Core.SmokeTests.SmokeTestEntryPoint` as the executable startup object.
- `SmokeTestEntryPoint` invokes the existing private `Program.Main()` without changing the legacy smoke list or per-test behavior.
- `TargetInvocationException` is unwrapped to the original exception and reported as `FAIL smoke runner: <type>: <message>`.
- all other entry-point exceptions are also converted to deterministic console failure + exit code `1` rather than being rethrown.
- unexpected/missing legacy `Program.Main()` signature/result fails closed with exit code `1`.
- `scripts/preflight-core-smoke-entrypoint.py` locks the guarded startup object, reflection exception containment, original-exception diagnostics, non-zero failure path, and continued registered-smoke execution.

## Excluded scope preserved

- No smoke assertion was weakened/skipped/reinterpreted.
- No issue #1105 / Curtain Family source was edited.
- No LOCAL_ONLY BricsCAD probe/runner was touched.
- `scripts/package-v25.ps1` remains unchanged and still packages only the plugin/Core payload plus reviewed scripts/samples, not the smoke executable.
- No GitHub Actions dispatch.

## Validation evidence

Read-back on `main` after `61df0a3d28757e39dfc92fbf4f1cae6e4b968d48` confirms:

- csproj blob `2daa76901678c5b38d38d890c6850f9e4d914dee` selects the guarded startup object;
- entry-point blob `01c8302b8c3c4ffe6af79cd8f4865ed664454fc2` catches/unpacks `TargetInvocationException`, prints original type/message and returns `1` without rethrow;
- guard blob `38ebb69d4cdc519f1420534603827ffb12ff046f` asserts those contracts and verifies legacy `Program.Main()` + `SmokeTestRegistration.RunAll()` remain enabled.

The current execution container has no `dotnet` binary, so the exact managed executable was not run here. Full smoke PASS and Windows popup disappearance therefore remain `PENDING_FRESH_SMOKE`; this claim does not invent runtime/CI PASS.

## Completion condition

Remote source containment is complete and pushed to `main`. A fresh Windows/.NET smoke execution should confirm that a future real assertion failure appears as deterministic console output/exit `1` without the Windows `0xe0434352` application-error popup; the suite itself must still pass independently before release qualification.