# Work claim — template category definedness

- Status: `COMPLETED`
- Agent: `chatgpt-gpt56sol-template-category-definedness-20260811-2330`
- Registered: `2026-08-11T23:30:18+07:00`
- Completed: `2026-08-11T23:33:33+07:00`
- Baseline main SHA: `5364d543ce9115b24f54b7727ea3b3797a14e701`
- Claim commit: `af48767ae38f3d0577fbcc178d331ffa2afeca4e`
- Implementation commit: `18f905c3b33e9e2604a654d23fa008fcc67a53d4`
- Regression-test commit: `5db90f8d7e99f6c9eadba801ebf439362bc029c1`
- Priority: deterministic malformed-template boundary defect found during owner-requested `continue all` review

## Reserved scope

Require persisted template family/rule category text to parse to a **defined** `ElementCategory`, not merely a numeric enum value accepted by `Enum.TryParse`.

## Implemented

- `TemplateProfileStore.Load` now requires both successful enum parse and `Enum.IsDefined` for persisted family categories before constructing `ProjectFamily`.
- The same definedness guard now applies to persisted quantity-rule categories before constructing `QuantityRule`.
- Malformed numeric category text is therefore rejected at the template XML/data boundary with `InvalidDataException` instead of leaking a later domain `ArgumentOutOfRangeException`.
- Existing valid template save/load/apply behavior is unchanged.

## Changed surfaces

- `src/QS3D.Core/Templates/TemplateProfileStore.cs`
- `tests/QS3D.Core.SmokeTests/WorkflowPersistenceSmoke.cs`
- this claim file

## Excluded scope

- No template Name changes (the prior name-integrity lane is completed).
- No native template commands/UI, BricsCAD V25 runtime qualification, family merge policy, quantity rule expression semantics, recognition engine changes or persistence atomicity redesign.
- No GitHub Actions dispatch/re-run and no LOCAL_PASS claim.

## Defect evidence

Before the fix, numeric text such as `"999"` passed `Enum.TryParse` even though `ElementCategory` did not define that value. Hardened `ProjectFamily` / `QuantityRule` constructors then rejected it later, causing malformed persisted template data to escape the loader as a domain construction exception instead of being classified at the template boundary.

## Validation performed

- Published claim `af48767ae38f3d0577fbcc178d331ffa2afeca4e` on current `main` before source/test changes.
- Re-fetched current `main` after claim publication and verified the claim remained in ancestry with `behind_by=0`; concurrent commits were disjoint.
- Re-fetched both reserved blobs and used their exact blob SHAs for conflict-safe writes.
- Source fix committed as `18f905c3b33e9e2604a654d23fa008fcc67a53d4`; regression committed as `5db90f8d7e99f6c9eadba801ebf439362bc029c1`.
- Regression creates valid templates through `TemplateProfileStore.Save`, corrupts only the emitted family/rule category to numeric `999`, and verifies `TemplateProfileStore.Load` throws `InvalidDataException` for each while the existing valid round-trip/apply regression remains in place.
- Compared the claim to then-current `main` `a7d76d2fff461cced33ba6110e0abdbc065e0e40`: status `ahead`, `ahead_by=34`, `behind_by=0`; both source/test changes remained reachable amid concurrent disjoint commits.
- No GitHub Actions workflow was dispatched or re-run. No hosted smoke execution or BricsCAD V25 runtime qualification is claimed.

## Outcome

Undefined numeric family/rule categories now fail closed as malformed template data at the file boundary, preserving a consistent loader error contract and preventing domain-construction exceptions from leaking out of corrupted template XML.