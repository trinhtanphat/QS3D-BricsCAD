# Work claim — QSDB QuantityRule text canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-qsdb-rule-text-canonicality`
- Registered: `2026-08-12T09:18:00+07:00`
- Baseline main SHA: `734cdec416cc507ced64c9d6a8927872deb61a40`
- Priority: P1 deterministic persistence / fail-closed token integrity found during owner-requested `continue all` audit.

## Confirmed defect

`QuantityRule` has canonicalized all required textual fields with `Trim()` since at least `f497819ad7de4178e42f25c070c38ac77b850412`, and `QsdbProjectStore.Serialize(...)` persists the resulting `id`, `output`, `expression` and `version`. `QsdbProjectXmlSchemaValidator` already requires canonical `id` and `output`, but it only allowlists `expression` and `version`; it does not require them to be non-empty/canonical. During `Load()`, the `QuantityRule` constructor trims those fields, so a current-schema file with padded `expression` or `version` is accepted and silently rewritten on the next save.

## Reserved scope

- `src/QS3D.Core/Persistence/QsdbProjectXmlSchemaValidator.cs`
- one focused Core smoke source under `tests/QS3D.Core.SmokeTests/`
- this claim file for close-out

## Plan

1. Re-fetch moving `main`, current validator and this claim before writes.
2. Require `expression` and `version` through the existing `ValidateRequiredCanonicalAttribute(...)` helper alongside `id`/`output`.
3. Do not change expression grammar/evaluation, rule identity/output uniqueness, version semantics, or rule engine behavior.
4. Add smoke coverage that saves a canonical QuantityRule, verifies round-trip, then independently pads persisted `expression` and `version` and requires `Load()` to fail closed.
5. Read back source/test on current `main`; no GitHub Actions or BricsCAD runtime PASS.
6. Close claim only after source/regression remain visible on current `main`.

## Excluded

- No rule-engine/provenance/preview/UI changes.
- No QSDB names/ids/categories/numeric/timestamp/dirty/changeVersion/migration changes.
- No BricsCAD adapter or installer/release work.
