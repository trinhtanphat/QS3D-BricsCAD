# Agent Work Claim — bundled BLT quantity compatibility preset

- Status: `ACTIVE`
- Agent: ChatGPT Web / GPT-5.6 Sol
- Registered: 2026-08-12 08:15 +07:00
- Baseline `main`: `c41355816f8a6f5aa4f38875efef30e20d34eb24`
- Scope refined after source audit: 2026-08-12 08:18 +07:00
- Priority: user-requested BLT3D calculation-settings parity

## Confirmed gap

The supplied `BLT3D_CaiDatTinhToan.json` is schema 2 and contains 28 category rules plus a complete directed 28×28 (784-row) intersection-deduction matrix. QS3D can import this external JSON manually, but does not ship the user-supplied matrix as a built-in preset. Current native `CreateDefault()` intentionally owns QS3D-native categories and conservative deduction defaults.

A deeper audit found that replacing native first-run defaults wholesale would be unsafe: the BLT matrix uses legacy integer categories (including specialized labels such as Dầm HCN/Giằng tường/Lanh tô/Sàn đặc/Đường dốc and additional unknown numeric codes), while runtime compatibility lookup deliberately falls back only for exact known label matches. Therefore the safe parity path is an explicit bundled BLT compatibility preset, not an automatic replacement of native first-run defaults.

## Reserved scope

- Add a pure-Core built-in BLT compatibility template derived exactly from the supplied JSON.
- Preserve all 28 legacy integer category rules and all 784 directed deduction rows without alias inference.
- Expose an explicit QS3DSETUP action to load the bundled BLT preset in-memory; persistence still occurs only through the existing Save Settings flow.
- Focused smoke/static regression proving cardinality, scalar values, representative directional flags, and that native `CreateDefault()` remains unchanged/conservative.

## Explicit exclusions

- Do not replace `QuantityCalculationSettings.CreateDefault()` or auto-activate BLT rules on first run.
- Do not change manually-created Category/Intersection Rule defaults; new user-authored rules remain conservative/OFF.
- Do not change Quantity Settings persistence, backup rotation, future-schema handling, stale-save guards, health/export behavior, or schema badge work.
- Do not alter deduction planner geometry/math.
- Do not infer, rename, or map unknown legacy numeric category codes.
- No GitHub Actions dispatch or BricsCAD V25 runtime claim from this remote lane.

## Validation plan

- Verify bundled BLT preset contains exactly 28 category rules and 784 directed intersection rules.
- Pin schema/scalar defaults to the supplied template.
- Pin representative asymmetric/directional deduction rows from the supplied source.
- Verify `CreateDefault()` still builds native categories with conservative directed deductions.
- Verify QS3DSETUP loads the preset into the existing editor and requires existing Save Settings persistence.
- Read back merged source from current `main` after concurrent changes.

## Completion condition

Implementation and focused regression are present on `main`, QS3DSETUP can load the canonical bundled BLT preset without an external file, native first-run defaults remain intact, concurrent Quantity lanes remain untouched, and this claim is changed to `COMPLETED` with merged SHAs recorded.
