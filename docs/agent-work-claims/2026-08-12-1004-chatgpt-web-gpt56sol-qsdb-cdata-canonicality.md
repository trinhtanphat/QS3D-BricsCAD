# Work claim — QSDB CDATA canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-qsdb-cdata-canonicality-20260812-1004`
- Registered: `2026-08-12T10:04:00+07:00`
- Baseline main SHA: `1ad3b37c638d2a5fe1b294132c3d37de3bf97797`
- Priority: P1 — reject non-canonical CDATA nodes at the current-schema QSDB persistence boundary.

## Confirmed defect

`QsdbProjectXmlSchemaValidator.ValidateElement(...)` handled `XText` before distinguishing `XCData`. Because `XCData` derives from `XText`, elements that opt into text (`<h>` source handles and `<d>` dependency ids) accepted CDATA. `QsdbProjectStore.Load()` then materialized the same value and a later save emitted ordinary text, silently canonicalizing a malformed/non-canonical persisted representation instead of failing closed. The license XML boundary already rejects CDATA explicitly, while current QSDB serialization never emits CDATA.

## Reserved surfaces

- `src/QS3D.Core/Persistence/QsdbProjectXmlSchemaValidator.cs` — reject `XCData` before the `XText` branch
- `tests/QS3D.Core.SmokeTests/QsdbCDataCanonicalitySmoke.cs` — focused load-boundary regression
- `tests/QS3D.Core.SmokeTests/QsdbCDataCanonicalityRegistration.cs` — smoke registration
- this claim file

## Implemented fix

- `14ccb751abf6e5893df619b1a81f6b9b09909b96` — reject `XCData` before generic `XText` handling for every QSDB element.
- `3beb7f3181b931e075c06a8c5c55a833fd57389e` — add real `QsdbProjectStore.Load()` coverage for ordinary text plus source-handle/dependency CDATA rejection.
- `2b42086f53c87111c40566e7f30858248ebbec7a` — register the focused smoke with the Core smoke executable.
- Readback on concurrent HEAD `cfa6f0ceb889e2f4003f4282339fdda038a504cb` confirmed the production guard and both regression cases remained present.

## Coordination

Family/Floor mutation freshness, generated-rebar handle, viewport padding, Units override, release30 preflight and other concurrent lanes remained outside this scope. No native/UI/recovery files were modified.

## Validation boundary

Exact GitHub source/readback review completed. A local `dotnet run` attempt could not start because the execution container could not resolve `github.com` to clone the repository, so no executable smoke/build PASS is claimed. No GitHub Actions were dispatched. No licensed BricsCAD V25/V26 runtime PASS is claimed remotely.
