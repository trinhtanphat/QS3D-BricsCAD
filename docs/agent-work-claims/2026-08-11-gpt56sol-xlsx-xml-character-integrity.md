# Agent Work Claim — XLSX XML character integrity

- Status: `RELEASED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11` (UTC+7)
- Released: `2026-08-11` (UTC+7)
- Baseline main SHA: `fbb7fae24e2fb2086715aba071aa8946e88e47ad`

## Confirmed defect

Three Core XLSX exporters built worksheet XML manually and called `SecurityElement.Escape(...)` for inline-string values. XML escaping protected markup characters such as `&`, `<` and `>`, but did not remove XML 1.0-forbidden control characters or isolated surrogate code units. A user/model value containing such a character could therefore make the generated worksheet part not well-formed.

Affected exporters:

- `MaterialUsageXlsxExporter`
- `CurtainWallXlsxExporter`
- `DoorOpeningXlsxExporter`

## Released scope

- `src/QS3D.Core/Export/XlsxXmlText.cs`
- `src/QS3D.Core/Export/MaterialUsageXlsxExporter.cs`
- `src/QS3D.Core/Export/CurtainWallXlsxExporter.cs`
- `src/QS3D.Core/Export/DoorOpeningXlsxExporter.cs`
- `tests/QS3D.Core.SmokeTests/XlsxXmlCharacterIntegritySmoke.cs`
- `tests/QS3D.Core.SmokeTests/XlsxXmlCharacterIntegritySmokeRegistration.cs`
- this claim file.

## Completed changes

- Claim registered before implementation: `48a23082d5d45b8bd2d135e0695e0d8873ae30c3`.
- Shared XML text boundary helper: `4c32ebdd98e1588b795ee6a6dd590ce913672f55`.
  - preserves XML 1.0-valid BMP characters, tab/CR/LF and valid surrogate pairs;
  - replaces forbidden controls, isolated surrogates and `U+FFFE/U+FFFF` with `U+FFFD`;
  - applies XML escaping only after character normalization.
- Material XLSX string cells switched to the helper: `a341906d4d320f72c137d8f5e7fe0af2772cd4f5`.
- Curtain-wall XLSX string cells switched to the helper: `a6888d4edf25a8d97c2e6ae33d0f42151c5007d8`.
- Door/opening XLSX string cells, including joined element/host IDs, switched to the helper: `fd243624d9d9f0312c9437e8789e4a562b8690e2`.
- End-to-end smoke coverage added: `096438eaaed13852580a0151fbfd7b4266879072`.
  - builds all three XLSX variants with text containing XML markup, `U+0001`, an isolated high surrogate and a valid emoji;
  - requires forbidden controls to be absent, replacement characters to remain, valid supplementary Unicode to survive and markup to remain escaped;
  - reparses generated worksheet XML with `XmlReader`.
- Smoke registration added: `0cf30ea71611677c696fa16608333a49f1c794ca`.

## Coordination / validation actually performed

- Exact exporter blobs were read from live `main` after the claim landed and used for conflict-safe updates.
- The shared helper and focused smoke were re-read from current `main` after implementation and contain the intended character policy.
- Current Export tree was re-read and confirms these are the three Core `*XlsxExporter.cs` surfaces in that directory and that the shared helper is present.
- GitHub Actions were not dispatched.
- A local validation attempt found `git` available but no `dotnet` SDK; network access from the shell also could not resolve GitHub, so the committed smoke executable/full solution could not be run locally in this session. No build/runtime PASS is claimed.
- No native BricsCAD/Windows qualification, release or installer operation was performed.

## Result

All three Core XLSX exporters now sanitize XML 1.0-invalid text at the serialization boundary while preserving valid Unicode and normal XML escaping. Hostile or corrupted text values can no longer invalidate the worksheet XML through these string-cell writers.
