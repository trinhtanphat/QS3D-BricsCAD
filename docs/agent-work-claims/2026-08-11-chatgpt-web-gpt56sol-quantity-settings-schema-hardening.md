# Work claim — Quantity settings schema/persistence hardening

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-quantity-settings-schema-hardening`
- Registered: `2026-08-11T21:00:00+07:00`
- Completed: `2026-08-11T21:03:00+07:00`
- Priority: P1

## Scope

- Audit the newly added per-user quantity settings persistence/template workflow for schema-version correctness and lossless save/export behavior.
- Ensure successfully loaded older supported settings are written back using the current schema version instead of retaining a stale schema marker after the user edits/saves/exports them.
- Preserve unknown compatibility category codes and all rule values; do not connect unconfirmed BLT intersection/formwork semantics into production quantity geometry.
- Add deterministic source regression coverage.

## Implemented

- `763aa1cfcf3112ca7ff2d36540f30e12e187f079` — `QuantitySettingsStore.Prepare(...)` now validates the incoming clone first (so future unsupported schemas still fail closed), then stamps `QuantityCalculationSettings.CurrentSchemaVersion` on the write copy used by both Save and Export.
- `de1a382411af64127ddaf59e019485f962eb42ab` — `QuantitySettingsWindow.BuildSettingsFromView()` now constructs edited settings directly at the current schema instead of retaining an older imported schema marker in memory.
- Rule/category/intersection payloads remain cloned and preserved; no compatibility category mapping or quantity geometry semantics were changed.
- `1e8227e72ff54c1fd6daf4c32121b09339370d0b` — added auto-discovered `scripts/preflight-quantity-settings-schema.py` guarding validate-before-stamp ordering, current-schema UI construction, rule payload retention tokens, and removal of the stale schema-preservation patterns.

## Validation

- The aggregate preflight runner discovers every `scripts/preflight-*.py`, so the new schema gate is automatically included in repository aggregate feature preflight runs.
- GitHub exposes no combined status checks for the preflight commit; this lane did not dispatch Actions or claim native BricsCAD V25 runtime PASS.
- Future schema inputs still fail in `NormalizeAndValidate()` before any current-schema stamping can hide the incompatibility.

## Remaining product boundary

- The Setup & Rules UI/persistence is implemented, but production intersection/formwork arithmetic is intentionally not switched to compatibility rules until the exact source/target subtraction semantics and remaining category-code mapping are confirmed. This lane does not guess engineering behavior.

## Completion evidence

- Save/export now always emits the current supported schema after successful validation while preserving the settings payload.
- Implementation/test tip: `1e8227e72ff54c1fd6daf4c32121b09339370d0b`; concurrent main commits were preserved and no force push was used.
