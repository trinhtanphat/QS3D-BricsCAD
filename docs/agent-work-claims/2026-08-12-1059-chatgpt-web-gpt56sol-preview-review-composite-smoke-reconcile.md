# Work claim — Preview Review composite smoke reconciliation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-preview-review-composite-smoke-reconcile-20260812-1059`
- Registered: `2026-08-12T10:59:00+07:00`
- Priority: P1 Core smoke contract reconciliation

## Confirmed regression

The completed Preview Review composite-row-key regression intentionally constructs U+001F-bearing `(ElementId, Field)` pairs and computes verified snapshot fingerprints to prove the comparison key cannot collide. The later completed Preview Review XML-text safety lane now rejects U+001F in persisted snapshot text from `ComputeFingerprint(...)`. The old smoke therefore throws during fixture construction and no longer tests the current contract.

## Reserved scope

- `tests/QS3D.Core.SmokeTests/PreviewReviewCompositeRowKeySmoke.cs`
- this claim file

## Intended reconciliation

- keep direct regression coverage that the private comparison `RowKey(...)` remains collision-free for separator-bearing tuple components;
- explicitly assert separator-bearing snapshots are no longer verified under the XML-text contract;
- preserve the valid case-insensitive comparison identity regression;
- do not modify Preview Review production source.

## Related completed work

- comparison collision source: `09d44d9d24acfd8bfaaca7173245568940d5b7de`
- original comparison regression: `8b05e1108bbcbd809bb3459d2a614aa80ec77e54`
- XML-text source: `337b0b3dc6c5c1dcb3e0f913ad4436a01bf03331`
- XML-text close: `9576dea47c55073d23e3cc8cb57de61fb9240f33`
- superseded source claim cleanup: `068039ab2eb553a1e116b636566962fe1b92062f`

## Validation boundary

Exact source/test readback only. No GitHub Actions/full build or BricsCAD V25/V26 runtime PASS claimed without execution.
