# Work claim — Quantity Settings negative schema fail-closed

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-quantity-settings-negative-schema`
- Registered: `2026-08-12T07:26:00+07:00`
- Completed: `2026-08-12T07:40:00+07:00`
- Baseline main SHA: `a0acf67ad9b6f777840840e20915ca9750c6dfb8`
- Priority: P2 evidence-driven remote-safe settings integrity

## Confirmed defect

`QuantityCalculationSettings.NormalizeAndValidate()` treated every `SchemaVersion <= 0` as an omitted legacy schema and silently upgraded it to `CurrentSchemaVersion`. A missing DataContract integer naturally deserializes as `0`, so preserving the zero-value compatibility path is intentional; a negative schema version, however, is explicit malformed state and was being normalized into a valid current-schema object instead of failing closed.

This allowed corrupted/programmatic negative schema metadata to bypass the schema integrity boundary before runtime lookup/store consumers clone and validate the settings.

## Implemented

- `8ed328fe918d57af8eeb5e10353f9cc6414e52ae` — registered this claim on `main` before source work.
- `7dacf58519b3cf7c128ba973f504631d2052db1f` — split schema validation so negative values throw `InvalidOperationException("Quantity settings schema cannot be negative.")`, schema `0` retains the legacy/missing-member upgrade to `CurrentSchemaVersion`, and future-schema validation remains unchanged.
- `a70ccd6b966fbbf18816d152f18cb0092586005b` — added focused Core smoke coverage for zero-schema compatibility, negative-schema rejection without caller mutation, `QuantityCalculationRuleSet` rejection of the same malformed settings, and current-schema validation.
- `f449fc2ed9cb1beafd09867647b5527a34deee73` — registered the focused smoke through `ModuleInitializer`.
- `3134625a1ea1b8bb3bde47d6a90ac2db8f526091` — added an auto-discovered static preflight that locks the negative → zero → future validation order and prevents reintroduction of `SchemaVersion <= 0` silent promotion.

## Preserved behavior

- Schema `0` still normalizes to `CurrentSchemaVersion` for missing/legacy DataContract compatibility.
- Current-schema and future-schema behavior are otherwise unchanged.
- No schema-version bump was introduced.
- No Quantity Settings Store/WPF/export/recovery behavior changed.
- No category/intersection defaults, BLT mapping, deduction planner or native geometry behavior changed.

## Validation performed

- Re-fetched the current source, smoke, smoke registration and focused preflight from `main` and inspected the final contract.
- Confirmed the source rejects negative schema values before the zero compatibility branch and before the future-schema check.
- Confirmed the smoke asserts exact negative-schema error text, caller non-mutation, runtime `QuantityCalculationRuleSet` rejection, schema-zero compatibility and current-schema validity.
- Compared final preflight commit `3134625a1ea1b8bb3bde47d6a90ac2db8f526091` to later `main` `25eabe095a07291b83cc52ad4a5f0e05134bf557`: later `main` is 50 commits ahead and the compare contains no subsequent changes to this lane's source/test/registration/preflight files.
- No GitHub Actions workflow was dispatched.
- This remote pass does **not** claim that the .NET smoke suite, focused Python preflight, local build or BricsCAD V25/V26 runtime was executed in a real checkout.

## LOCAL_ONLY disposition

This is a Core schema-validation invariant and requires no new local-only queue item. Existing licensed/native BricsCAD qualification boundaries remain unchanged; no remote V25/V26 runtime PASS is claimed.

## Completion evidence

Negative Quantity Settings schema metadata can no longer be silently promoted to the current schema, while the existing missing-schema zero compatibility remains intact. Final source commit: `7dacf58519b3cf7c128ba973f504631d2052db1f`; smoke: `a70ccd6b966fbbf18816d152f18cb0092586005b`; registration: `f449fc2ed9cb1beafd09867647b5527a34deee73`; focused preflight: `3134625a1ea1b8bb3bde47d6a90ac2db8f526091`.