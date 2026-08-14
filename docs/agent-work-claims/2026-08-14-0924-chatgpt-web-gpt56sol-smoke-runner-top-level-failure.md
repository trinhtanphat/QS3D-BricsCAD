# Work claim — Smoke runner top-level failure containment

- Status: `SOURCE_FIXED / PENDING_FRESH_SMOKE`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-14T09:24:00+07:00`
- Baseline main SHA: `0aea8c2a986bd443eb73b7f456082a540489cfa9`
- Evidence source: Google Sheet `TEST QS3D`, Ver 6 testcase, screenshot showing `QS3D.Core.SmokeTests.exe - Application Error` with CLR exception code `0xe0434352`.
- Claim commit: `e98c30fb79abe41e0f9df6b5cd1d175152453675`
- Scope amendment: `b3d14f8114892de4f8f4d6fdee18aca8f5650ebe`
- Initial guarded entry-point fix: `61df0a3d28757e39dfc92fbf4f1cae6e4b968d48`
- Direct `Program.Main()` containment hardening: `d0cdc8113c101725b316dacd1eceaf727e665348`
- Direct containment regression guard: `69a00926c952672adb73d8ed384b19d31ba5b0e1`

## Reserved scope

Prevent the Core smoke executable from terminating through an uncaught registered-smoke exception and surfacing as a Windows application-error popup. Preserve a non-zero process exit for failed smoke validation while printing the actual failing exception to the console.

## Implemented

Two containment layers now protect the same invariant without weakening smoke assertions:

1. `QS3D.Core.SmokeTests.csproj` selects `QS3D.Core.SmokeTests.SmokeTestEntryPoint` as the executable startup object. `SmokeTestEntryPoint` invokes the legacy private `Program.Main()`, unwraps `TargetInvocationException`, reports the original failure and returns exit code `1` instead of rethrowing through the Windows process boundary.
2. `Program.Main()` itself now wraps `SmokeTestRegistration.RunAll()` in a top-level `try/catch`. A registered-smoke failure is printed as `FAIL registered smoke phase: <type>: <message>` plus stack trace when present, then returns `1`. The exception is not rethrown into a Windows application-error popup path.

Existing per-test `Test(...)` collection remains unchanged: ordinary legacy smoke failures are still accumulated and make the process return non-zero. No assertion is skipped or reinterpreted.

Regression guards:

- `scripts/preflight-core-smoke-entrypoint.py` locks the guarded startup object / reflection boundary.
- `scripts/preflight-smoke-runner-failure-containment.py` locks the direct registered-smoke `try/catch`, original exception diagnostics, exit `1`, absence of `throw;`, and continued per-test collection.

## Excluded scope preserved

- No smoke assertion was weakened/skipped/reinterpreted.
- No Curtain Family or unrelated production source was edited by this lane.
- No LOCAL_ONLY BricsCAD probe/runner was touched.
- Packaging remains separate from the smoke executable.
- No workflow gate was lowered to manufacture a green result.

## Validation evidence

Remote read-back confirms both containment layers and both focused guards are on `main`. The most recent observed successful V25 cloud release run is still run `31781825194` / #160 on older SHA `6d834dbadc4c13ce4f7966fbaea00cf1ec8499bb`, so it cannot be reused as evidence for the newer smoke changes.

The connected GitHub surface currently exposes workflow inspection/retry but no fresh workflow-dispatch action, and this environment does not provide independent exact Windows/.NET execution evidence for the resulting current SHA. Therefore full suite PASS and disappearance of the historical Windows `0xe0434352` dialog remain `PENDING_FRESH_SMOKE` rather than being inferred from source.

## Completion condition

Remote source containment is complete and pushed to `main`. A fresh Windows/.NET execution built from the exact target SHA must still confirm both of these independent requirements:

1. the smoke suite itself passes all assertions; and
2. if a future registered assertion intentionally fails, it is surfaced as deterministic console diagnostics + process exit `1` without a Windows application-error popup.

Until that execution exists, this claim intentionally remains `SOURCE_FIXED / PENDING_FRESH_SMOKE`.
