# Work claim — QSDB XML text preflight

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-qsdb-xml-text-preflight-20260812-1036`
- Registered: `2026-08-12T10:36:00+07:00`
- Completed: `2026-08-12T10:39:00+07:00`
- Priority: P1 persistence atomicity / malformed-state safety

## Confirmed defect

`QsdbProjectStore.SaveCore(...)` called `ValidateProject(...)`, then created the destination directory before serializing the project. `ValidateProject(...)` validated canonical keys/references and numeric/timestamp invariants, but did not preflight every string emitted to QSDB XML. XML-invalid control characters or malformed surrogate text could therefore fail during XML serialization only after destination filesystem mutation.

## Resolution

- Claim: `4e4fbe21b49ddd5a1a97b59d525436a8062c062b`
- Source: `85b3676ba23edfbf9046675f7e0a96c1f2e2b57c`
- Regression: `f46cf5d99c13048e94d4fe0c39a87fe20407e2cb`

`SaveCore(...)` now preflights the fully materialized in-memory QSDB XML before path directory/temp-file mutation and verifies both attribute values and text nodes with `XmlConvert.VerifyXmlChars`. XML representability failures are surfaced as `InvalidDataException`. The focused smoke covers invalid metadata attribute text, invalid relation text nodes, lone surrogate data, no filesystem/project-persistence mutation on failure, and valid supplementary Unicode/null metadata round-trip semantics.

## Validation boundary

Focused source-safe regression + exact source/test readback only. No GitHub Actions/full build/executable smoke or BricsCAD V25/V26 runtime PASS claimed.
