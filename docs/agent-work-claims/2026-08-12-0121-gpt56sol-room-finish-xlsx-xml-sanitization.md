# Work claim — Room Finish XLSX XML text sanitization

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-room-finish-xlsx-xml-20260812-0121`
- Registered: `2026-08-12T01:21:00+07:00`
- Baseline main SHA: `139bfdec84ff34ab16470d57ab3a0b3d10b4f682`
- Priority: P2 evidence-driven remote-safe XLSX integrity hardening

## Confirmed defect

`RoomFinishXlsxExporter.StringCell(...)` still uses `SecurityElement.Escape(...)`. That escapes XML markup but does not apply the repository's XLSX XML-character policy. `XlsxXmlText.Escape(...)`, already used by Material, Curtain and Door exporters, preserves valid surrogate pairs and replaces XML-invalid control characters or unpaired surrogates with U+FFFD. Room Finish therefore remains an inconsistent path where otherwise accepted row text can make temporary worksheet XML invalid and cause export failure instead of producing the sanitized package used by neighboring XLSX exporters.

## Reserved scope

Switch only Room Finish inline-string serialization to the shared `XlsxXmlText.Escape(...)` policy and add focused regression coverage for XML-invalid control/unpaired-surrogate text while preserving valid text and the structural row/cell bounds from the immediately preceding completed claim.

## Expected surfaces

- `src/QS3D.Core/Export/RoomFinishXlsxExporter.cs`
- `tests/QS3D.Core.SmokeTests/RoomFinishXlsxXmlSanitizationSmoke.cs`
- this claim file

## Excluded scope

- No row/cell limit changes.
- No `XlsxXmlText.cs` shared helper changes.
- No `RoomFinishSchedule.cs` grouping/business logic.
- No Door/Material/Curtain/Quantity/Rebar exporters.
- No UI/native BricsCAD/runtime or GitHub Actions work.

## Validation plan

- Export a row containing XML-invalid control text and an unpaired surrogate without failing package validation.
- Inspect worksheet XML and require U+FFFD replacement while preserving ordinary neighboring text.
- Preserve current 32,767-character checks, row bound, numeric finite validation and atomic publish behavior.
- SHA-guard writes and re-read current `main` after integration.

## Coordination

The active Room Finish schedule group-key claim explicitly excludes XLSX. The immediately preceding Room Finish XLSX structural-limit claim is completed and this claim does not alter its bounds. Neighboring Material/Curtain/Door exporters already use the shared sanitizer.

## Completion condition

Completed only after Room Finish inline strings use the shared sanitizer, focused regression source is present on current `main`, exact SHAs are recorded, and the claim is marked `COMPLETED`.
