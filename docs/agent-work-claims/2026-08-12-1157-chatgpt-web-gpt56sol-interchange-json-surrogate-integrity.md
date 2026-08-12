# Work claim — Interchange JSON surrogate integrity

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-interchange-json-surrogate-integrity-20260812-1157`
- Registered: `2026-08-12T11:57:00+07:00`
- Completed: `2026-08-12T12:08:00+07:00`
- Baseline main SHA: `efff914eeba9604511cff876895496f180a2e7fb`
- Priority: P1 — semantic interchange export must not silently replace invalid UTF-16 input while validating/persisting JSON.
- Task Key: `CORE-INTERCHANGE-JSON-SURROGATE-INTEGRITY`

## Confirmed defect

`ProjectInterchangeJsonExporter.Escape(...)` copied UTF-16 surrogate code units verbatim. A caller-visible semantic string containing an unpaired high or low surrogate could therefore survive in the string returned by `Build(...)`, while the later UTF-8 conversion used by validation/export could replace that invalid code unit with U+FFFD. The validated/persisted artifact could differ from the source semantic value instead of failing closed. Valid paired surrogates (for example emoji/supplementary Unicode) are legitimate and remain supported.

## Reserved scope

- `src/QS3D.Core/Export/ProjectInterchangeJsonExporter.cs`
- `tests/QS3D.Core.SmokeTests/ProjectInterchangeJsonSurrogateIntegritySmoke.cs`
- this claim file

## Implemented contract

- `Escape(...)` now walks UTF-16 by index and rejects an unpaired high or low surrogate with `InvalidDataException` before canonical JSON validation or file publication.
- A valid high+low surrogate pair is appended intact and remains valid supplementary Unicode.
- Existing JSON escaping, ordering, semantic reference validation, file publication, size limits and format/version behavior are unchanged.
- Interchange import/merge semantics, timestamp fixtures, native/UI code and public format version were not changed.

## Evidence

- Claim reservation: `8bbc49b090a98027ba9ebce87dc88b70f52d3199`
- Source fix: `e6f56ebe33a331ff4abaa1588566551752432296`
- Focused auto-registered Core smoke: `35e752640f65da71d49b06cf73df919343b1994c`

## Validation evidence

- Exact source diff review confirmed the source commit changes only `ProjectInterchangeJsonExporter.Escape(...)` surrogate handling.
- Focused smoke covers lone high-surrogate rejection, lone low-surrogate rejection, and preservation/canonical validation of a valid supplementary Unicode surrogate pair.
- Ancestry checks after publication showed source and smoke commits are ancestors of current `main` with `behind_by=0`.
- This connector session did not execute the full Core smoke executable, GitHub Actions, or licensed BricsCAD V25/V26 runtime/build qualification; no PASS claim is made for those environments.

## Completion condition

`COMPLETED`: the unpaired-surrogate integrity hole is fixed on `main`, focused deterministic smoke coverage is committed, exact publication SHAs are recorded, and remote validation limitations are explicit.
