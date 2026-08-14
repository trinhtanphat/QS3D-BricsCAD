# Work claim — Preview Review XML-valid tab fixture reconciliation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-preview-review-xml-valid-tab`
- Registered: `2026-08-14T13:51:30+07:00`
- Baseline main SHA: `445c69c9ccef189dd513b6b3518401dc5c0d5b44`
- Priority: continue the deterministic registered Core smoke blocker chain after the completed invalid-provenance fixture reconciliation.

## Fresh blocker evidence

The completed `2026-08-14-codex-preview-review-xml-rule-provenance-fixture.md` closeout records that the full registered Core smoke now advances through `InvalidRuleProvenanceFailsBeforeSnapshot` and stops at the independent `PreviewReviewXmlTextSafetySmoke.XmlValidWhitespaceRoundTripsExactly` fixture because that case still uses a tab-bearing `QuantityRule` ID that current persistability validation no longer accepts at construction time.

## Reserved scope

- `tests/QS3D.Core.SmokeTests/PreviewReviewXmlTextSafetySmoke.cs`
- this claim file

## Implementation boundary

- Test-fixture reconciliation only unless source readback proves a production defect; do not relax `QuantityRule` identity validation merely to admit an obsolete test fixture.
- Preserve the actual XML-valid whitespace round-trip contract under test. If the tab is meant to exercise XML text rather than semantic identity validity, move that whitespace to the intended serializable review text/value surface while keeping rule identity valid and canonical.
- Keep the existing invalid XML/control-character fail-closed coverage intact.
- Do not modify Level/Curtain, Beam/Rebar native-smoke gate, release workflow, BricsCAD runtime, private data, or unrelated Core/domain surfaces.
- Do not dispatch GitHub Actions; CI remains manual-only.

## Validation plan

1. Read back the current smoke method and `QuantityRule` validation contract.
2. Make the narrowest correction that still proves XML-valid whitespace round-trips exactly.
3. Read back the committed diff and verify the implementation remains on refreshed `main` ancestry.
4. Close this claim with exact implementation evidence.
5. Do not claim licensed BricsCAD runtime PASS from this Core test fixture.
