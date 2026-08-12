# Work claim — Quantity XLSX XML text sanitization

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-quantity-xlsx-xml-20260812-0718`
- Registered: `2026-08-12T07:18:00+07:00`
- Baseline main SHA: `815b1cc6f329dbd9700583aa666431b0bef6e692`
- Priority: P2 evidence-driven remote-safe XLSX integrity hardening

## Confirmed defect

`XlsxQuantityExporter.AppendInlineStringCell(...)` used `SecurityElement.Escape(...)`. That escaped XML markup but did not apply the repository's shared XLSX XML-character policy. `XlsxXmlText.Escape(...)`, already used by neighboring exporters, preserves valid surrogate pairs and replaces XML-invalid controls/unpaired surrogates with U+FFFD.

## Reserved scope

Switch only Quantity XLSX inline-string serialization (standard and ED2 share the same helper) to `XlsxXmlText.Escape(...)` and add focused regression coverage while preserving the completed worksheet/cell structural bounds and all ED2 semantic/numeric parity rules.

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

## Validation implemented

- Standard worksheet source regression covers XML-invalid control text, an unpaired surrogate and XML markup escaping.
- ED2 regression exercises the same shared serializer in both `CHI_TIET` and `TONG_HOP` worksheet XML.
- Source commit readback confirms the exporter diff is exactly the serializer call replacement with no structural/business-rule changes.
- Current smoke source was re-read from `main` after integration.

## Integration commits

- Claim: `bf273b8899033324ad64368f322d7a697d10fcf9`
- Serializer fix: `2965418b77b790cecc158ff75f5a71f6ee71f80b`
- Focused standard/ED2 smoke: `f3e5e11a16c2f4ac2fc22ee4f7dbc29e987431ef`

## Validation boundary

Remote source/smoke review only. No .NET build, BricsCAD V25/V26 runtime qualification, private-DWG/native execution or GitHub Actions run is claimed by this session.

## Coordination

The Quantity XLSX structural-limits claim is completed at `2dee205ec1e1ebe8cdbbdf8e703b9c61dd78699f`. No independent Quantity XLSX sanitizer owner was found before registration. This claim remained serializer-only.

## Completion condition

Completed: Quantity XLSX inline strings use the shared sanitizer, focused standard/ED2 regression source is present on current `main`, and exact integration SHAs are recorded above.
