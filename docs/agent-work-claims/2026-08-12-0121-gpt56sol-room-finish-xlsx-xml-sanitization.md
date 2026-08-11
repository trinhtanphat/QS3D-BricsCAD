# Work claim — Room Finish XLSX XML text sanitization

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-room-finish-xlsx-xml-20260812-0121`
- Registered: `2026-08-12T01:21:00+07:00`
- Completed: `2026-08-12T01:24:00+07:00`
- Baseline main SHA: `139bfdec84ff34ab16470d57ab3a0b3d10b4f682`
- Claim commit: `c48cf943548ba7c5521d9a4679d56683fa0df966`
- Source fix commit: `a63294151debbc120c2122f84076ffe3e706303b`
- Regression commits: `8e79c604af66df54e0a5b0661e2cedf84a437fda`, `0ef5ef4b1a245644544b7cb0fce85f3cd7170e2b`
- Priority: P2 evidence-driven remote-safe XLSX integrity hardening

## Confirmed defect

`RoomFinishXlsxExporter.StringCell(...)` still used `SecurityElement.Escape(...)`. That escaped XML markup but did not apply the repository's XLSX XML-character policy. `XlsxXmlText.Escape(...)`, already used by neighboring exporters, preserves valid surrogate pairs and replaces XML-invalid control characters or unpaired surrogates with U+FFFD. Room Finish could therefore fail temporary worksheet XML validation for otherwise accepted row text instead of producing the sanitized package used by neighboring XLSX exporters.

## Implemented

- `RoomFinishXlsxExporter.StringCell(...)` now uses `XlsxXmlText.Escape(value)`.
- Removed the now-unused `System.Security` import.
- Structural row/cell limits from the preceding completed claim remain unchanged.
- Added module-initializer smoke `RoomFinishXlsxXmlSanitizationSmoke` that exports text containing U+0001, an unpaired high surrogate, and XML markup characters; it opens the generated worksheet and requires U+FFFD replacement plus normal `&lt;`/`&amp;` escaping, with no invalid control/surrogate remaining.
- Follow-up regression commit replaced `string.Contains(..., StringComparison)` with `IndexOf(..., StringComparison)` to keep the smoke compatible with broader target-framework surfaces without changing semantics.

## Implemented surfaces

- `src/QS3D.Core/Export/RoomFinishXlsxExporter.cs`
- `tests/QS3D.Core.SmokeTests/RoomFinishXlsxXmlSanitizationSmoke.cs`
- this claim file

## Excluded scope honored

- No row/cell limit changes.
- No `XlsxXmlText.cs` shared helper changes.
- No `RoomFinishSchedule.cs` grouping/business logic.
- No Door/Material/Curtain/Quantity/Rebar exporters.
- No UI/native BricsCAD/runtime or GitHub Actions work.

## Validation actually performed

- Claim commit was current `main` before implementation; exact current exporter blob was re-fetched and SHA-guarded before the one-path serializer change.
- Current source was re-read after implementation and confirms `StringCell(...)` routes through `XlsxXmlText.Escape(...)` while structural limit helpers remain present.
- The focused smoke was re-read after its target-compatibility follow-up.
- At checkpoint, latest regression commit `0ef5ef4b1a245644544b7cb0fce85f3cd7170e2b` was current `main`, so source and smoke were integrated in direct ancestry.
- No force push/reset/revert was used.
- No local .NET smoke execution is claimed in this connector-only lane.
- No BricsCAD V25/V26 runtime or GitHub Actions execution is claimed.

## Coordination

The active Room Finish schedule group-key claim explicitly excludes XLSX. The immediately preceding Room Finish XLSX structural-limit claim was completed first and this claim did not alter its bounds. Neighboring Material/Curtain/Door exporters remained untouched.

## Completion condition

Completed. Room Finish inline strings now use the shared XML sanitizer, focused regression source is present on current `main`, exact integration commits are recorded, and the claim is released.
