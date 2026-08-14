# Work claim — Preview Review XML rule-provenance fixture reconciliation

- Status: `COMPLETED`
- Agent: `codex-preview-review-xml-rule-provenance-fixture-20260814` (`/root/fix_level_curtain_frame_z`, delegated by `/root`)
- Registered: `2026-08-14T13:46:08+07:00`
- Completed: `2026-08-14T13:50:05+07:00`
- Baseline main SHA: `e64cda101cbdd8f58d196cba44775ba2171d8660`
- Priority: continue the independent Core smoke fixture reconciliation after the Measurement coverage fix

## Diagnosis

`PreviewReviewXmlTextSafetySmoke.InvalidRuleProvenanceFailsBeforeSnapshot` directly constructs a `QuantityRule` whose ID contains U+0001. Current `QuantityRule.RequiredToken` correctly rejects control characters at the public persistence-integrity boundary, so module initialization fails before the fixture can exercise the separately intentional Preview Review XML fail-closed boundary.

The completed Preview Review XML text-safety claim deliberately drives invalid text through real quantity-rule provenance and requires `PreviewReviewSnapshotService.Create` to reject it as invalid XML. The later completed QuantityRule token-persistability claim made that provenance unreachable through supported construction without superseding the snapshot boundary's defense-in-depth contract.

## Reserved scope

- `tests/QS3D.Core.SmokeTests/PreviewReviewXmlTextSafetySmoke.cs`
- this claim file
- parent LOCAL-003 claim only for the explicit delegation/completion record

Inside `InvalidRuleProvenanceFailsBeforeSnapshot`, generate a valid preview and assert its real canonical rule provenance, then use bounded test-local reflection to corrupt only the resulting `QuantityRulePreviewChange.AfterProvenance` backing field to XML-invalid text. Assert the injected value reached the preview change, then retain the existing `PreviewReviewSnapshotService.Create` `InvalidOperationException` assertion containing `invalid in XML`.

## Excluded scope

No production rule/review/XML/domain/persistence change, no adjacent fixture, and no Level production, probe, runner, BricsCAD, private data, GitHub Actions, V26, release or packaging change.

## Validation and completion

Run the strict Core smoke Release build, registered full Core smoke, and relevant Preview Review/XML gates. If the complete smoke reaches a separate stale fixture, report it without expanding this claim. Merge the test-only correction through a normal PR, record exact SHAs, then mark this claim `COMPLETED`.

## Completion record

- Claim-only PR `#1175` merged as `2f1c78a2ccf2382c7a8ccb7c1e8b733e178b89c1` before the test edit.
- Implementation source commit `dd589a3d269295ba37e7f14f1b4c10f95efd2dd5` merged through PR `#1176` as `6a99f3d48dad7dc52580790f4bc5c7fb607e88d9`.
- The fixture now generates and verifies real canonical `cost-rule@1` preview provenance, injects U+0001 only into the immutable preview change's `AfterProvenance` backing field, proves the injection reached the snapshot input, and retains the Preview Review `invalid in XML` fail-closed assertion.
- Core smoke Release build passed with zero warnings/errors. All seven focused Preview Review, Quantity Rule preview/provenance and review lifecycle gates passed. The complete registered smoke advanced through this invalid-provenance case and then stopped at the independent `XmlValidWhitespaceRoundTripsExactly` tab-bearing QuantityRule ID fixture; that case remains unchanged and outside this claim.
- No production, domain, Level, probe/runner, BricsCAD, private-data or GitHub Actions surface was changed or executed.
