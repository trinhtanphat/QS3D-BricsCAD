# Work claim — Preview Review XML-valid tab fixture reconciliation

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-preview-review-xml-valid-tab`
- Registered: `2026-08-14T13:51:30+07:00`
- Completed: `2026-08-14T13:55:00+07:00`
- Baseline main SHA: `445c69c9ccef189dd513b6b3518401dc5c0d5b44`
- Implementation SHA: `07e494d80d935acea42e957e49ba3429dfd609d2`
- Priority: continue the deterministic registered Core smoke blocker chain after the completed invalid-provenance fixture reconciliation.

## Fresh blocker evidence

The completed `2026-08-14-codex-preview-review-xml-rule-provenance-fixture.md` closeout records that the full registered Core smoke advances through `InvalidRuleProvenanceFailsBeforeSnapshot` and stops at the independent `PreviewReviewXmlTextSafetySmoke.XmlValidWhitespaceRoundTripsExactly` fixture because that case still used a tab-bearing `QuantityRule` ID that current persistability validation correctly rejects at construction time.

## Reserved scope

- `tests/QS3D.Core.SmokeTests/PreviewReviewXmlTextSafetySmoke.cs`
- this claim file

## Implementation boundary

- Test-fixture reconciliation only; `QuantityRule` identity validation was not relaxed.
- Preserve the actual XML-valid whitespace round-trip contract under test.
- Keep the existing invalid XML/control-character fail-closed coverage intact.
- Do not modify Level/Curtain, Beam/Rebar native-smoke gate, release workflow, BricsCAD runtime, private data, or unrelated Core/domain surfaces.
- Do not dispatch GitHub Actions; CI remains manual-only.

## Completion record

- Source readback confirms `QuantityRule.RequiredToken` rejects control-character IDs and versions, so the previous `cost\tline` rule identity was no longer a valid way to construct this XML serializer fixture.
- `07e494d80d935acea42e957e49ba3429dfd609d2` now constructs the real preview with canonical `cost-line@1` provenance, verifies that canonical provenance first, then injects the XML-valid tab only into the immutable preview change's `AfterProvenance` backing field for the persistence test.
- The same fixture still verifies exact tab preservation in both the review name and persisted/reloaded provenance; the XML-invalid U+0001 fail-closed fixture remains unchanged.
- GitHub commit readback confirms the implementation touches only `tests/QS3D.Core.SmokeTests/PreviewReviewXmlTextSafetySmoke.cs`.
- The implementation is the refreshed `main` head observed before closeout; no force update was used.
- GitHub Actions were not dispatched. No licensed BricsCAD runtime PASS is claimed from this Core-only test correction.
