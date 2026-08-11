# Agent Work Claim — XLSX XML character integrity

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11` (UTC+7)
- Baseline main SHA: `fbb7fae24e2fb2086715aba071aa8946e88e47ad`

## Confirmed defect

Three Core XLSX exporters build worksheet XML manually and call `SecurityElement.Escape(...)` for inline-string values. XML escaping protects markup characters such as `&`, `<` and `>`, but it does not remove XML 1.0-forbidden control characters or isolated surrogate code units. A user/model value containing such a character can therefore make the generated worksheet part not well-formed. The package is subsequently parsed by the strict `XlsxPackageValidator`, so an otherwise valid export can fail because text was not normalized at the XML serialization boundary.

Affected exporters:

- `MaterialUsageXlsxExporter`
- `CurtainWallXlsxExporter`
- `DoorOpeningXlsxExporter`

## Reserved scope

- `src/QS3D.Core/Export/XlsxXmlText.cs` — shared XLSX XML 1.0 text sanitizer/escaper (new).
- `src/QS3D.Core/Export/MaterialUsageXlsxExporter.cs` — string-cell escaping only.
- `src/QS3D.Core/Export/CurtainWallXlsxExporter.cs` — string-cell escaping only.
- `src/QS3D.Core/Export/DoorOpeningXlsxExporter.cs` — string-cell escaping only.
- focused smoke coverage and registration under `tests/QS3D.Core.SmokeTests/`.
- this claim file for close-out evidence.

## Intended contract

- Preserve valid XML 1.0 text, including tab/CR/LF and valid supplementary Unicode represented by surrogate pairs.
- Replace XML 1.0-forbidden controls, noncharacters `U+FFFE/U+FFFF`, and isolated surrogate code units with Unicode replacement character `U+FFFD` instead of failing or silently truncating the entire value.
- Continue XML-escaping markup characters after character normalization.
- Preserve existing XLSX workbook/worksheet structure, calculations, grouping, numeric serialization and `xml:space="preserve"` behavior.

## Explicit non-overlap

- No report aggregation/business-calculation changes.
- No reporting reference-normalization changes.
- No persistence/session, BricsCAD runtime, updater, installer, signing or release workflow changes.
- No unrelated exporter refactor.

## Coordination / validation boundary

- Current claim-directory and exact filename searches showed no reservation for these exporter files before registration; GitHub code-search indexing reported incomplete results, so live `main` and exact target blobs will be re-read immediately before every source write.
- Add deterministic Core smoke coverage for control characters, markup escaping, valid supplementary Unicode and invalid surrogate handling.
- Do not dispatch GitHub Actions.
- No native BricsCAD/Windows runtime PASS is claimed by this remote lane.
