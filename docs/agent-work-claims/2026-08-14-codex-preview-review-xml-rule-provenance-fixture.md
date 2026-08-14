# Work claim — Preview Review XML rule-provenance fixture reconciliation

- Status: `ACTIVE`
- Agent: `codex-preview-review-xml-rule-provenance-fixture-20260814` (`/root/fix_level_curtain_frame_z`, delegated by `/root`)
- Registered: `2026-08-14T13:46:08+07:00`
- Baseline main SHA: `e64cda101cbdd8f58d196cba44775ba2171d8660`
- Priority: continue the independent Core smoke fixture reconciliation after the Measurement coverage fix

## Diagnosis

`PreviewReviewXmlTextSafetySmoke.InvalidRuleProvenanceFailsBeforeSnapshot` directly constructs a `QuantityRule` whose ID contains U+0001. Current `QuantityRule.RequiredToken` correctly rejects control characters at the public persistence-integrity boundary, so module initialization fails before the fixture can exercise the separately intentional Preview Review XML fail-closed boundary.

The completed Preview Review XML text-safety claim deliberately drives invalid text through real quantity-rule provenance and requires `PreviewReviewSnapshotService.Create` to reject it as invalid XML. The later completed QuantityRule token-persistability claim made that provenance unreachable through supported construction without superseding the snapshot boundary's defense-in-depth contract.

## Reserved scope

- `tests/QS3D.Core.SmokeTests/PreviewReviewXmlTextSafetySmoke.cs`
- this claim file
- parent LOCAL-003 claim only for the explicit delegation/completion record

Inside `InvalidRuleProvenanceFailsBeforeSnapshot`, construct the project with a valid rule ID, then use bounded test-local reflection to corrupt only that `QuantityRule.Id` backing field to `cost\u0001rule`. Assert the injected value reached the rule, then retain the real preview provenance check and the existing `PreviewReviewSnapshotService.Create` `InvalidOperationException` assertion containing `invalid in XML`.

## Excluded scope

No production rule/review/XML/domain/persistence change, no adjacent fixture, and no Level production, probe, runner, BricsCAD, private data, GitHub Actions, V26, release or packaging change.

## Validation and completion

Run the strict Core smoke Release build, registered full Core smoke, and relevant Preview Review/XML gates. If the complete smoke reaches a separate stale fixture, report it without expanding this claim. Merge the test-only correction through a normal PR, record exact SHAs, then mark this claim `COMPLETED`.
