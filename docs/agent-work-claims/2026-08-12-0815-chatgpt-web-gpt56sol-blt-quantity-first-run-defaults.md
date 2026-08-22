# Agent Work Claim — BLT quantity first-run defaults

- Status: `ACTIVE`
- Agent: ChatGPT Web / GPT-5.6 Sol
- Registered: 2026-08-12 08:15 +07:00
- Baseline `main`: `c41355816f8a6f5aa4f38875efef30e20d34eb24`
- Priority: user-requested BLT3D calculation-settings parity

## Confirmed gap

The supplied `BLT3D_CaiDatTinhToan.json` is schema 2 and contains 28 category rules plus a complete directed 28×28 (784-row) intersection-deduction matrix. Current first-run `QuantitySettingsStore.Load()` falls back to `QuantityCalculationSettings.CreateDefault()` when neither the primary settings file nor backup exists. That default reproduces the scalar thresholds but synthesizes native categories and all-new intersection rows with deduction flags off, so a fresh QS3D install does not inherit the supplied BLT calculation semantics unless the user manually imports the template.

## Reserved scope

- First-run/default Quantity Settings semantics derived from the supplied BLT3D template.
- Exact preservation of the 28 legacy integer category codes and 784 directed deduction rows.
- Focused Core regression/static preflight proving template cardinality and representative directional flags.
- Preserve schema version 2 and existing scalar defaults.

## Explicit exclusions

- Do not change manually-created Category/Intersection Rule defaults; new user-authored rules remain conservative/OFF.
- Do not change Quantity Settings persistence, backup rotation, future-schema handling, stale-save guards, health/export UI, or schema badge work.
- Do not alter deduction planner geometry/math beyond feeding it the canonical first-run rules.
- Do not infer or rename unknown legacy numeric category codes.
- No GitHub Actions dispatch or BricsCAD V25 runtime claim from this remote lane.

## Validation plan

- Verify the canonical default contains exactly 28 category rules and 784 directed intersection rules.
- Pin scalar defaults to the supplied template.
- Pin representative asymmetric/directional deduction rows from the supplied source.
- Verify manually-created rule code remains all-OFF by source/static gate.
- Read back merged source from current `main` after concurrent changes.

## Completion condition

Implementation and focused regression are present on `main`, the first-run loader reaches the canonical BLT defaults without external import, concurrent Quantity lanes remain untouched, and this claim is changed to `COMPLETED` with merged SHAs recorded.
