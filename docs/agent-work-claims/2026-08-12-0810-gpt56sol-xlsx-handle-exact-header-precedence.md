# Work claim — XLSX Handle reader exact-header precedence

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-xlsx-handle-exact-header-precedence-20260812-0810`
- Registered: `2026-08-12T08:10:00+07:00`
- Baseline main SHA: `68517455c46f688a74f4a1d6632c9b93e8d4bb3a`
- Priority: P2 evidence-driven remote-safe XLSX header-resolution correctness

## Confirmed defect

`XlsxHandleReader.ReadHandleLookup(...)` currently places both an exact `CAD Handle (hex)` header and every fuzzy header containing `handle` into the same `handleColumns` set. A valid modern QS3D worksheet can therefore be rejected as ambiguous merely because it also contains an unrelated descriptive column such as `Handle Notes`; the modern schema gate sees two Handle columns even though one exact semantic Handle header is present.

## Reserved scope

Give exact `CAD Handle (hex)` headers precedence over fuzzy compatibility headers. Use fuzzy `contains handle` columns only when no exact Handle header was discovered. Preserve duplicate-exact-header rejection, legacy/fuzzy compatibility when no exact header exists, explicit Handle precedence over `$decimal`, and modern Element ID/fingerprint schema checks.

## Expected surfaces

- `src/QS3D.Core/Export/XlsxHandleReader.cs`
- `tests/QS3D.Core.SmokeTests/XlsxHandleExactHeaderPrecedenceSmoke.cs`
- this claim file

## Excluded scope

- No redesign of fuzzy legacy header matching beyond exact-header precedence.
- No XLSX exporter changes or BLT/ED2 handle semantics changes.
- No UI/native BricsCAD/runtime, persistence or GitHub Actions work.

## Validation plan

- A modern QS3D row with exact `CAD Handle (hex)` plus unrelated `Handle Notes` must resolve the exact Handle column and remain modern.
- Two exact `CAD Handle (hex)` headers must still fail the modern schema ambiguity guard.
- A legacy/non-modern sheet with only a fuzzy Handle header must remain readable through the existing fuzzy compatibility path.
- Re-read source/test after SHA-guarded integration and preserve concurrent history.

## Coordination

Recent searches found no active XlsxHandleReader header-resolution owner. The previous Boolean-cell claim is completed. This claim is limited to exact-vs-fuzzy Handle header precedence.

## Completion condition

Completed only when exact Handle headers cannot be made ambiguous by unrelated fuzzy Handle headers, fuzzy compatibility remains covered without an exact header, duplicate exact headers remain rejected, focused regression source is on current `main`, exact integration SHAs are recorded and this claim is marked `COMPLETED`.
