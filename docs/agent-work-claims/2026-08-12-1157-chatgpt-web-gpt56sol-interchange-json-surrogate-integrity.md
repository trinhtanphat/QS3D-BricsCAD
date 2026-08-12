# Work claim — Interchange JSON surrogate integrity

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-interchange-json-surrogate-integrity-20260812-1157`
- Registered: `2026-08-12T11:57:00+07:00`
- Baseline main SHA: `efff914eeba9604511cff876895496f180a2e7fb`
- Priority: P1 — semantic interchange export must not silently replace invalid UTF-16 input while validating/persisting JSON.
- Task Key: `CORE-INTERCHANGE-JSON-SURROGATE-INTEGRITY`

## Confirmed defect

`ProjectInterchangeJsonExporter.Escape(...)` currently copies UTF-16 surrogate code units verbatim. A caller-visible semantic string containing an unpaired high or low surrogate can therefore survive in the string returned by `Build(...)`, while the later UTF-8 conversion used by validation/export may replace that invalid code unit with U+FFFD. The validated/persisted artifact can differ from the source semantic value instead of failing closed. Valid paired surrogates (for example emoji/supplementary Unicode) are legitimate and must remain supported.

## Reserved scope

- `src/QS3D.Core/Export/ProjectInterchangeJsonExporter.cs`
- one focused auto-registered Core smoke file for interchange surrogate integrity
- this claim file

## Intended contract

- Reject an unpaired UTF-16 high or low surrogate before canonical JSON validation or file publication.
- Preserve valid surrogate pairs and ordinary Unicode exactly through `Build(...)`/JSON validation.
- Preserve all existing JSON escaping, ordering, semantic reference validation, file publication, size limits and format/version behavior.
- Do not change interchange import/merge semantics, timestamp fixtures, native/UI code or public format version.

## Validation plan

Add focused deterministic Core smoke coverage proving lone high/low surrogate rejection and a valid supplementary Unicode pair remains present in the built snapshot and passes canonical validation.

## Validation boundary

No GitHub Actions will be dispatched. No licensed BricsCAD V25/V26 runtime/build PASS will be claimed unless actually executed.
