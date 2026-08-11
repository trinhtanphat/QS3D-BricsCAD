# Work claim — Quantity Settings cardinality bounds

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-quantity-settings-cardinality-20260811-2325`
- Registered: `2026-08-11T23:25:00+07:00`
- Completed: `2026-08-11T23:36:00+07:00`
- Baseline main SHA: `f3ab6727e8f5fbefc9596d312ae9193f31346875`
- Priority: P1 — harden imported/edited Quantity Settings against unbounded category universes and quadratic matrix-diagnostic allocation while preserving exact unknown integer-code compatibility.

## Delivered scope

- `src/QS3D.Core/Reporting/QuantityCalculationSettings.cs`
- `tests/QS3D.Core.SmokeTests/QuantityCalculationSettingsCardinalitySmoke.cs`
- `tests/QS3D.Core.SmokeTests/QuantityCalculationSettingsCardinalitySmokeRegistration.cs`
- `scripts/preflight-quantity-settings-cardinality.py`
- this claim file

## Implemented contract

- `QuantityCalculationSettings.MaxObservedCategoryCodeCount` is explicitly bounded at 256 distinct observed category codes.
- `MaxDirectedIntersectionRuleCount` is the corresponding 65,536 directed-pair ceiling.
- `NormalizeAndValidate()` rejects oversized `CategoryRules` and `IntersectionRules` before traversing those collections.
- Validation tracks the union of category-rule codes plus every intersection Source/Target and fails closed as soon as the distinct observed universe exceeds 256, so later matrix diagnostics cannot allocate an unbounded observed-code cross product.
- Duplicate directed pairs now use an exact packed `(Source, Target)` `long` key instead of allocating composite strings; direction remains significant.
- Unknown non-negative integer codes remain exact and supported inside the bound, including values outside the native `ElementCategory` enum. No enum cast or alias inference was introduced.

## Regression coverage

- Current/default settings remain valid.
- A full 28-code imported-style directed matrix (784 rules) remains valid.
- Exact unknown integer codes including `1301` and `int.MaxValue` remain valid and directional.
- Exactly 256 observed category codes remain valid.
- 257 category rules fail closed.
- 65,537 intersection rules fail through the early collection-count guard.
- A sparse intersection list with only 129 rules but 258 distinct observed codes fails through the distinct-universe guard.
- Focused static preflight pins both early count guards, incremental observed-code guarding, exact pair-key semantics and the no-enum-inference/no-payload-mutation boundary.

## Product integration

- Claim registration: `83b4ef19c4e731329674cf5e7bee7c8d53ec0b47`.
- PR: `#531` — `fix(quantity): bound settings matrix cardinality`.
- Squash merge on `main`: `09e3749a856b8d246f46f42e121289df5f3ecb8f`.
- The implementation branch was refreshed with current `main` through a merge commit before PR merge after direct fast-forward attempts correctly failed when concurrent agents advanced `main`; no force push was used and concurrent winners were preserved.

## Validation actually performed

- Re-fetched the final `QuantityCalculationSettings.cs` from post-merge `main`; its blob is `324a09ef6d5bba17d24e857884bf908ce368cc88` and contains the cardinality guards and exact pair key.
- Re-fetched the final smoke source from post-merge `main`; its blob is `1a88e54697477994228fa30c8562565db08a918e` and contains all seven registered boundary scenarios.
- Source/static review only in this remote session. The new smoke/preflight were not executed from a repository checkout here, so no execution PASS is claimed.
- No GitHub Actions or release workflow was dispatched. No licensed BricsCAD V25 runtime PASS is claimed or required for this Core-only source boundary.

## Coordination

The concurrent Quantity Settings create-rule UI lane owns WPF XAML/code-behind and explicitly excludes Core quantity models/arithmetic; no overlapping UI/store files were touched. Other concurrent ownership/material/grid/persistence/release work also remained untouched.

## Remaining boundary

This hardening bounds imported/settings payload size only. Native CAD intersection measurement, face/contact classification, engulf behavior, overlap precedence and double-deduction prevention remain separate semantic/runtime work and must not be inferred from field names alone.

## Completion

Reservation released. Quantity Settings validation now prevents unbounded category-universe/matrix amplification while preserving current/default, 28-code imported-style and exact unknown integer-code behavior.
