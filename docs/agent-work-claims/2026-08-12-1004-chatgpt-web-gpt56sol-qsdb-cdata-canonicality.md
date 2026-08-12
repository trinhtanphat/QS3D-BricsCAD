# Work claim — QSDB CDATA canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-qsdb-cdata-canonicality-20260812-1004`
- Registered: `2026-08-12T10:04:00+07:00`
- Baseline main SHA: `1ad3b37c638d2a5fe1b294132c3d37de3bf97797`
- Priority: P1 — reject non-canonical CDATA nodes at the current-schema QSDB persistence boundary.

## Confirmed defect

`QsdbProjectXmlSchemaValidator.ValidateElement(...)` handles `XText` before distinguishing `XCData`. Because `XCData` derives from `XText`, elements that opt into text (`<h>` source handles and `<d>` dependency ids) currently accept CDATA. `QsdbProjectStore.Load()` then materializes the same value and a later save emits ordinary text, silently canonicalizing a malformed/non-canonical persisted representation instead of failing closed. The license XML boundary already rejects CDATA explicitly, while current QSDB serialization never emits CDATA.

## Reserved surfaces

- `src/QS3D.Core/Persistence/QsdbProjectXmlSchemaValidator.cs` — reject `XCData` before the `XText` branch
- `tests/QS3D.Core.SmokeTests/QsdbCDataCanonicalitySmoke.cs` — focused load-boundary regression
- `tests/QS3D.Core.SmokeTests/QsdbCDataCanonicalityRegistration.cs` — smoke registration
- this claim file

## Intended fix

- Reject `XCData` for every QSDB element before generic text handling.
- Preserve ordinary text for `<h>` and `<d>` and existing whitespace/canonical-value validation.
- Cover both source-handle and dependency CDATA with real `QsdbProjectStore.Load()` cases, plus an ordinary-text control that still loads.
- Do not alter migration semantics, XML namespaces, persistence format, native/UI code, or recovery behavior.

## Coordination

Current Family/Floor mutation freshness, generated-rebar handle, viewport padding, Units override, release30 preflight and other active lanes are outside this scope. No overlap is intended.

## Validation boundary

Committed deterministic Core smoke coverage plus exact source/diff review. No GitHub Actions dispatch; no licensed BricsCAD V25/V26 runtime PASS claimed remotely.
