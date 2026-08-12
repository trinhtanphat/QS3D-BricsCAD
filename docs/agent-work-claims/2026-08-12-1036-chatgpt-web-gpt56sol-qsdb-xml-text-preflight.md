# Work claim — QSDB XML text preflight

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-qsdb-xml-text-preflight-20260812-1036`
- Registered: `2026-08-12T10:36:00+07:00`
- Priority: P1 persistence atomicity / malformed-state safety

## Confirmed defect

`QsdbProjectStore.SaveCore(...)` calls `ValidateProject(...)`, then creates the destination directory before serializing the project. `ValidateProject(...)` validates canonical keys/references and numeric/timestamp invariants, but does not preflight every string that will be emitted to QSDB XML. XML-invalid control characters or malformed surrogate text in serializable project state can therefore fail during `Serialize(...)`/XML writing only after the destination filesystem has already been mutated.

## Reserved scope

- `src/QS3D.Core/Persistence/QsdbProjectStore.cs`
- one focused auto-registered Core smoke under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Intended contract

Preflight the fully serialized in-memory XML text before destination-directory/temp-file mutation. Convert XML representability failures to `InvalidDataException`, preserve all existing canonicality/reference/numeric/schema rules, preserve successful QSDB serialization semantics including valid supplementary Unicode and null map values, and keep failed saves from changing project schema/version/timestamp or filesystem state.

## Validation boundary

Focused source-safe regression + exact readback only. No GitHub Actions/full build/executable smoke or BricsCAD V25/V26 runtime PASS claimed without execution.
