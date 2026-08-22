# Work claim — Preview Review composite smoke reconciliation

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-preview-review-composite-smoke-reconcile-20260812-1059`
- Registered: `2026-08-12T10:59:00+07:00`
- Completed: `2026-08-12T11:01:00+07:00`
- Priority: P1 Core smoke contract reconciliation

## Confirmed regression

The completed Preview Review composite-row-key regression intentionally constructed U+001F-bearing `(ElementId, Field)` pairs and computed verified snapshot fingerprints to prove the comparison key cannot collide. The later completed Preview Review XML-text safety lane now rejects U+001F in persisted snapshot text from `ComputeFingerprint(...)`. The old smoke therefore threw during fixture construction and no longer tested the current contract.

## Resolution

- Claim: `8162f77e16d4aed27281738a972fac9ee023848b`
- Regression reconciliation: `ef84b2d332fee8a2f0b9d28a484fd86c21abfccf`

`PreviewReviewCompositeRowKeySmoke` now directly invokes the private comparison `RowKey(...)` via reflection to preserve collision-free tuple-key coverage without constructing an invalid persisted artifact. It separately verifies that a separator-bearing snapshot fails `PreviewReviewSnapshotService.Verify(...)` under the new XML-text contract, and preserves the valid case-insensitive comparison identity regression.

Exact readback confirmed current `PreviewReviewSnapshotComparisonService.RowKey(...)` remains length-prefixed and the reconciled smoke is present on moving `main`.

## Related completed work

- comparison collision source: `09d44d9d24acfd8bfaaca7173245568940d5b7de`
- original comparison regression: `8b05e1108bbcbd809bb3459d2a614aa80ec77e54`
- XML-text source: `337b0b3dc6c5c1dcb3e0f913ad4436a01bf03331`
- XML-text close: `9576dea47c55073d23e3cc8cb57de61fb9240f33`
- superseded source claim cleanup: `068039ab2eb553a1e116b636566962fe1b92062f`

## Validation boundary

Exact source/test readback only. No GitHub Actions/full build or BricsCAD V25/V26 runtime PASS claimed.
