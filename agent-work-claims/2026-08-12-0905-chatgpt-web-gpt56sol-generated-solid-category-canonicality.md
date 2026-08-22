# Work claim — Generated Solid category canonicality

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-generated-solid-category-canonicality`
- Registered: `2026-08-12T09:05:00+07:00`
- Completed: `2026-08-12T09:15:00+07:00`
- Baseline main SHA: `2f1e11adb8faab79214a36c763cdb171342f7b03`
- Priority: P1 — persisted Generated Solid category metadata must match the exact writer-owned enum token.
- Task Key: `CORE-MODEL-HEALTH-GENERATED-SOLID-CATEGORY-CANONICALITY`

## Confirmed defect

`GeneratedGeometryService.CommitReplacement(...)` persists `GeneratedSolidCategory` with exact `category.ToString()`. `ModelHealthService.ValidateGeneratedGeometry(...)` used case-insensitive enum parsing and only reported a mismatch when the parsed enum differed from the semantic element category. Case-varied, padded, or numeric aliases that parsed to the same category could therefore pass baseline health even though the writer never emits those spellings.

## Implemented

- Claim commit: `06fd8269ae166e1db049ff4f847b46987a3a7756`
- Branch source commit: `4f801d8fed09f2fc0146dcb755d04365238549a0`
- Branch smoke commit: `23047487c3a53a65de1c48083a51e430cc194a59`
- PR: `#679`
- Squash merge on `main`: `c058ce3b198181cc6736768c8dd06ad658e1839c`

`ModelHealthService.ValidateGeneratedGeometry(...)` now normalizes only for parsing, derives the writer-owned canonical token with `generatedCategory.ToString()`, and emits `GENERATED_CATEGORY_NON_CANONICAL` when the persisted raw value differs. Existing `GENERATED_CATEGORY_MISSING` and `GENERATED_CATEGORY_MISMATCH` behavior remains intact.

## Regression coverage

`ModelHealthGeneratedSolidCategoryCanonicalitySmoke` covers case-varied, padded and numeric aliases, canonical mismatch preservation, and exact canonical control behavior.

## Validation

- Read back current `ModelHealthService.cs` and the focused smoke from merged `main`.
- Compared squash merge `c058ce3b198181cc6736768c8dd06ad658e1839c` to later `main` `c022c99918782e2091eb8514a48224d6e0376c90`: status `ahead`, `ahead_by=2`, `behind_by=0`, merge base exactly the squash commit; later changes were unrelated.
- No GitHub Actions workflow was dispatched. No full .NET build or licensed BricsCAD V25/V26 runtime PASS is claimed from this remote lane.
