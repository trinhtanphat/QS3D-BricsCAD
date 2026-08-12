# Work claim — Quantity XLSX XML text sanitization

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-xlsx-xml-20260812-0718`
- Registered: `2026-08-12T07:18:00+07:00`
- Baseline main SHA: `815b1cc6f329dbd9700583aa666431b0bef6e692`
- Priority: P2 evidence-driven remote-safe XLSX integrity hardening

## Confirmed defect

`XlsxQuantityExporter.AppendInlineStringCell(...)` still uses `SecurityElement.Escape(...)`. That escapes markup but does not apply the repository's shared XLSX XML-character policy. `XlsxXmlText.Escape(...)`, already used by the neighboring Material, Curtain and Room Finish exporters, preserves valid surrogate pairs and replaces XML-invalid controls/unpaired surrogates with U+FFFD. Quantity XLSX therefore remains a path where accepted report text can make worksheet XML invalid instead of producing the sanitized package used by neighboring exporters.

## Reserved scope

Switch only Quantity XLSX inline-string serialization (standard and ED2 share the same helper) to `XlsxXmlText.Escape(...)` and add focused regression coverage for XML-invalid control/unpaired-surrogate text while preserving the just-completed worksheet/cell structural bounds and all ED2 semantic/numeric parity rules.

## Expected surfaces

- `src/QS3D.Core/Export/XlsxQuantityExporter.cs`
- `tests/QS3D.Core.SmokeTests/XlsxQuantityXmlSanitizationSmoke.cs`
- this claim file

## Excluded scope

- No row/cell structural-bound changes.
- No changes to `XlsxXmlText.cs`.
- No reporting/grouping/quantity business semantics.
- No Door/Material/Curtain/Room Finish/Rebar exporters.
- No UI/native BricsCAD/runtime or GitHub Actions work.

## Validation plan

- Standard Quantity XLSX exports text containing an XML-invalid control and unpaired surrogate, replacing both with U+FFFD while preserving normal text and escaping XML markup.
- Exercise the shared serializer on ED2 as well without weakening ED2 identity/numeric parity.
- Re-read current source/test after integration and preserve concurrent history with SHA-guarded writes.
- Source/smoke review only; no .NET or BricsCAD runtime PASS unless actually executed.

## Coordination

The Quantity XLSX structural-limits claim is completed at `2dee205ec1e1ebe8cdbbdf8e703b9c61dd78699f`. Recent commit searches found no independent Quantity XLSX sanitizer owner. This claim is serializer-only.

## Completion condition

Completed only after Quantity XLSX inline strings use the shared sanitizer, focused standard/ED2 regression source is present on current `main`, exact integration SHAs are recorded and this claim is marked `COMPLETED`.
