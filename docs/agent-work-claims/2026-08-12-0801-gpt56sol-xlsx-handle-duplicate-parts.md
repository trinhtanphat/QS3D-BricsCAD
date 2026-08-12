# Work claim — XLSX Handle reader duplicate critical-part integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-xlsx-handle-duplicate-parts-20260812-0801`
- Registered: `2026-08-12T08:01:00+07:00`
- Baseline main SHA: `4e08d2c671039ee7509ccd5bc51db8495ef52248`
- Priority: P2 evidence-driven remote-safe XLSX package-integrity hardening

## Confirmed defect

`XlsxHandleReader` used `ZipArchive.GetEntry(...)` to resolve critical package parts. .NET documents that when multiple ZIP entries have the same name, `GetEntry(...)` returns the first one, while duplicate names can exist in a ZIP. A malformed XLSX could therefore carry conflicting copies of a critical part and the reader silently consumed one by archive order.

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

## Validation implemented

- `GetUniqueEntry(...)` now fails closed on duplicate exact critical parts and is used for workbook XML, workbook relationships, shared strings, declared worksheet targets and exact fallback `sheet1.xml`.
- Metadata-free fallback detects duplicate FullNames among candidate worksheet parts before selecting the existing first distinct worksheet.
- Focused smoke creates real duplicate ZIP entries and covers duplicate workbook, sharedStrings, declared worksheet and fallback sheet1 parts.
- The smoke also preserves distinct worksheet fallback and intentionally includes a duplicate unrelated `notes/readme.txt`, proving this is not a blanket ZIP duplicate prohibition.
- Source diff was re-read and is limited to critical-part uniqueness resolution.
- Smoke commit remains an ancestor of current `main`; the only subsequent commit touched an unrelated release preflight claim.

## Integration commits

- Claim: `1bff80a0e35152dfd86e48fd6f3ae646e48470c5`
- Source fix: `3956e4a521b1a100c9368aedc09023170a88d44a`
- Focused smoke: `0d8585b10d8de98b6a54929b6c38a4ff0d9d3ad6`

## Evidence

Microsoft Learn documents that `ZipArchive.GetEntry(name)` returns the first entry when duplicate names exist and that duplicate ZIP entry names can be created.

## Validation boundary

Remote source/smoke review only. No .NET build, BricsCAD V25/V26 runtime qualification, private-DWG/native execution or GitHub Actions run is claimed by this session.

## Coordination

No active duplicate-XLSX-part owner was found before registration. The preceding explicit Handle precedence claim is completed; this claim remained package-part resolution only.

## Completion condition

Completed: duplicate consumed critical parts fail closed, focused regression source is on current `main`, and exact integration SHAs are recorded above.
