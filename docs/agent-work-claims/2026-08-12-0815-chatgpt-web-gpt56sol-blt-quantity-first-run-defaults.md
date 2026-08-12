# Agent Work Claim — bundled BLT quantity compatibility preset

- Status: `COMPLETED`
- Agent: ChatGPT Web / GPT-5.6 Sol
- Registered: 2026-08-12 08:15 +07:00
- Baseline `main`: `c41355816f8a6f5aa4f38875efef30e20d34eb24`
- Scope refined after source audit: 2026-08-12 08:18 +07:00
- Completed: 2026-08-12 08:23 +07:00
- Priority: user-requested BLT3D calculation-settings parity

## Confirmed gap

The supplied `BLT3D_CaiDatTinhToan.json` is schema 2 and contains 28 category rules plus a complete directed 28×28 (784-row) intersection-deduction matrix. QS3D could import this external JSON manually, but did not ship the user-supplied matrix as a built-in preset. Native `CreateDefault()` intentionally owns QS3D-native categories and conservative deduction defaults.

A deeper audit found that replacing native first-run defaults wholesale would be unsafe: the BLT matrix uses legacy integer categories (including specialized labels such as Dầm HCN/Giằng tường/Lanh tô/Sàn đặc/Đường dốc and additional unknown numeric codes), while runtime compatibility lookup deliberately falls back only for exact known label matches. The completed implementation therefore uses an explicit bundled BLT compatibility preset rather than replacing native first-run defaults.

## Completed implementation

- `242a0995765dc2b47616789ad0bd6f92ad25f67e` — `feat(quantity): bundle BLT calculation preset`
  - Adds pure-Core `QuantityCalculationBltCompatibilityPreset.Create()`.
  - Preserves all 28 legacy integer category rules and all 784 directed deduction rows from the supplied JSON without alias inference.
  - Preserves schema 2 and all supplied scalar thresholds/dimension defaults.
- `231b3449bcce29faccd0ff16e123e98169e40803` — `fix(quantity): stage BLT preset after settings baseline`
  - Loads the preset only after the existing persisted-settings baseline has initialized, so Save/Discard/Cancel protection remains effective.
- `97549d56c61fd78138761565f4840ead938455a4` — `feat(quantity): add BLT preset setup command`
  - Adds `QS3DSETUPBLT`, opening the existing Setup & Rules editor with the bundled BLT preset staged in memory.
  - Persistence remains explicit through the existing `Lưu Cài Đặt` path.
- `4200277050bffa3389fe23d70dde2db74557c918` — `test(quantity): cover bundled BLT preset semantics`
  - Pins 28 category rules, 784 directed rules, scalar values, extraction examples, asymmetric rule examples, and native-default non-regression.
- `b9ccef654400f813705442b10c472dff5fff35ac` — `test(quantity): register bundled BLT preset smoke`
  - Registers the focused smoke in the existing Core smoke runner.

## Validation evidence

- Programmatic parity check against the supplied `BLT3D_CaiDatTinhToan.json` reconstructed and compared all 28/28 category extraction rules and 784/784 directed intersection rules exactly.
- Current-`main` readback after concurrent commits confirmed the Core preset source, `QS3DSETUPBLT` command, post-baseline UI staging, smoke source, and smoke registration remain present.
- Native `QuantityCalculationSettings.CreateDefault()` remains unchanged/conservative; the BLT preset is opt-in and does not replace first-run native settings.
- One expected GitHub non-fast-forward (`409`) occurred while `main` advanced concurrently; no force update was used. The command commit was safely retried after refreshing `main`.
- Combined status for regression commit `b9ccef654400f813705442b10c472dff5fff35ac` has no reported statuses and no workflow runs. No GitHub Actions were dispatched and no licensed BricsCAD V25/WPF runtime PASS is claimed from this remote lane.

## Explicit exclusions retained

- Manually-created Category/Intersection Rule defaults remain conservative/OFF.
- Quantity Settings persistence, backup rotation, future-schema handling, stale-save guards, health/export behavior, and schema badge work were not changed.
- Deduction planner geometry/math was not changed.
- Unknown legacy numeric category codes were not guessed, renamed, or mapped.

## Remaining optional input

The numeric BLT semantics are preserved exactly. For fully human-readable UI/native-category alignment, a source-of-truth category dictionary for the remaining legacy codes that QS3D does not currently name/map would be useful; those names should not be inferred.
