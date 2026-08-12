# Work claim — XLSX Handle reader duplicate critical-part integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-xlsx-handle-duplicate-parts-20260812-0801`
- Registered: `2026-08-12T08:01:00+07:00`
- Baseline main SHA: `4e08d2c671039ee7509ccd5bc51db8495ef52248`
- Priority: P2 evidence-driven remote-safe XLSX package-integrity hardening

## Confirmed defect

`XlsxHandleReader` uses `ZipArchive.GetEntry(...)` to resolve critical package parts such as `xl/workbook.xml`, `xl/_rels/workbook.xml.rels`, `xl/sharedStrings.xml` and worksheet targets. .NET documents that when multiple ZIP entries have the same name, `GetEntry(...)` returns the first one, and ZIP creation permits duplicate names. A malformed XLSX can therefore carry multiple conflicting copies of a critical part and the reader silently consumes one by archive order.

## Reserved scope

Fail closed when a package part actually consumed by the reader has duplicate entries with the same exact FullName. Apply uniqueness to workbook metadata, shared strings, explicitly resolved worksheet targets and the legacy exact `sheet1.xml` fallback. When metadata-free fallback searches arbitrary worksheet parts, reject duplicate FullNames among fallback worksheet candidates while preserving the existing first-distinct-sheet fallback behavior.

## Expected surfaces

- `src/QS3D.Core/Export/XlsxHandleReader.cs`
- `tests/QS3D.Core.SmokeTests/XlsxHandleDuplicatePartSmoke.cs`
- this claim file

## Excluded scope

- No blanket prohibition on duplicate unrelated ZIP entries.
- No relationship-target normalization redesign.
- No XLSX exporter changes or BLT/ED2 handle semantics.
- No UI/native BricsCAD/runtime, persistence or GitHub Actions work.

## Validation plan

- Duplicate `xl/workbook.xml` must fail closed before worksheet parsing.
- Duplicate `xl/sharedStrings.xml` must fail closed when shared strings are consumed.
- Duplicate declared worksheet target must fail closed.
- Duplicate exact metadata-free `xl/worksheets/sheet1.xml` must fail closed.
- Preserve ordinary unique-part packages and legacy fallback with distinct worksheet names.
- Re-read current source/test after SHA-guarded integration and preserve concurrent history.

## Evidence

Microsoft Learn documents that `ZipArchive.GetEntry(name)` returns the first entry when duplicate names exist; `ZipArchive.CreateEntry` permits creating a second entry with an existing name.

## Coordination

Recent search found no active duplicate-XLSX-part owner. The preceding explicit Handle precedence claim is completed; this claim is package-part resolution only.

## Completion condition

Completed only when duplicate consumed critical parts fail closed, focused regression source is on current `main`, exact integration SHAs are recorded and this claim is marked `COMPLETED`.
