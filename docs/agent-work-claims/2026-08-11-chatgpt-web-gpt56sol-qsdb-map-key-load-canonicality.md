# Agent work claim — QSDB map-key load canonicality

Status: COMPLETED

Agent: ChatGPT Web / GPT-5.6 Sol
Date: 2026-08-11 (UTC+7)
Baseline main SHA: `5d8f9c209e7f25b39510766f9c8f672ffd498679`
Claim commit: `5cf79385c52e2af3e74c19f8e67043898b2c2b29`
Implementation commit: `670a81e96612079141ea2f1ee99fb9911c37184c`
Regression commit: `f64b6297c040cdfe97c5f1d8ae448a5ef903db85`

## Completed

- `QsdbProjectXmlSchemaValidator.ValidateMap()` now rejects empty or whitespace-padded persisted map keys before loader normalization.
- The guard applies to project metadata, family properties, and element properties.
- Existing duplicate-key handling remains unchanged.
- `QsdbCanonicalPersistenceSmoke` now verifies a persisted metadata key with surrounding whitespace is rejected on load.
- Both changed files were fetched again from current `main` after the writes and reviewed.
- No GitHub Actions were dispatched; no BricsCAD runtime qualification was attempted.
