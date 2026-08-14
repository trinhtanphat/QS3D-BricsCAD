# Work claim — Smoke runner top-level failure containment

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-14T09:24:00+07:00`
- Baseline main SHA: `0aea8c2a986bd443eb73b7f456082a540489cfa9`
- Evidence source: Google Sheet `TEST QS3D`, Ver 6 testcase, screenshot showing `QS3D.Core.SmokeTests.exe - Application Error` with CLR exception code `0xe0434352`.

## Reserved scope

Prevent the Core smoke executable from terminating through an uncaught registered-smoke exception and surfacing as a Windows application-error popup. Preserve a non-zero process exit for failed smoke validation while printing the actual failing exception to the console.

## Reserved surfaces

- `tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj`
- new `tests/QS3D.Core.SmokeTests/SmokeTestEntryPoint.cs`
- read-only `tests/QS3D.Core.SmokeTests/Program.cs` and `SmokeTestRegistration.cs`
- one focused static regression/preflight under `scripts/`

The implementation deliberately avoids editing the large legacy `Program.cs`: the csproj selects a small guarded entry point that invokes the existing private `Program.Main()` and converts any escaping exception into deterministic console failure + exit code 1.

## Excluded scope

- Do not weaken, skip, or reinterpret any smoke assertion to make the suite pass.
- Do not edit the currently claimed issue #1105 Curtain Family smoke lane.
- Do not touch LOCAL_ONLY BricsCAD probes/runners.
- Do not alter release packaging to include smoke binaries; `scripts/package-v25.ps1` already packages only the plugin/Core DLLs plus reviewed scripts/samples.
- No GitHub Actions dispatch.

## Acceptance

- Any exception escaping the existing smoke runner is caught at the executable boundary.
- Failure output names the top-level smoke phase and includes the original exception type/message.
- Process exits non-zero on failure and does not rethrow the exception after reporting.
- Existing `Program.Main()` and per-test `Test(...)` behavior remain unchanged.
- The entry point fails closed if the legacy `Program.Main()` cannot be resolved or returns an unexpected type.
- A focused source guard proves the csproj selects the guarded entry point and the wrapper unwraps `TargetInvocationException`.

## Coordination

Refresh `main` immediately before each write. Abort/re-scope if another ACTIVE/BLOCKED claim reserves the csproj, the new entry-point path, or the new guard path.

## Completion condition

Source + focused regression are pushed to `main`, exact SHA is read back and the claim records source-fixed status. Full smoke PASS is only claimed if actually executed; no CI/native PASS is inferred.