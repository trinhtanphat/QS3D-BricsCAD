# Work claim — QSDB dirty-token canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-qsdb-dirty-token-canonicality`
- Registered: `2026-08-12T09:05:00+07:00`
- Baseline main SHA: `8f0b933ac03a737ec6dbc24d9b69aec7fd7677bd`
- Priority: P1 deterministic persistence / fail-closed token integrity found during owner-requested `continue all` audit.

## Confirmed defect

`QsdbProjectStore.Serialize(...)` emits element dirty flags as `((int)x.Dirty).ToString(CultureInfo.InvariantCulture)`. Historical source at `f497819ad7de4178e42f25c070c38ac77b850412` already used that exact canonical representation when dirty-state persistence was present. The loader's `Dirty(...)`, however, uses `int.TryParse(..., NumberStyles.Integer, ...)`, which accepts semantically equivalent noncanonical tokens such as `+1`, `01`, or padded integer text and converts them to the same enum value. A later save silently rewrites the token.

Legacy migration seeds a missing v1 dirty attribute with `((int)ElementDirtyFlags.All).ToString(CultureInfo.InvariantCulture)`, so enforcing canonical integer text does not reject values emitted or synthesized by supported repository writers/migrations.

## Reserved scope

- `src/QS3D.Core/Persistence/QsdbProjectStore.cs`
- one focused Core smoke source under `tests/QS3D.Core.SmokeTests/`
- this claim file for close-out

## Plan

1. Re-fetch moving `main`, current store/migrator and this claim before writes.
2. Preserve existing valid-range/bitmask checks in `Dirty(...)`, then require the original persisted token to exactly match `raw.ToString(InvariantCulture)` using ordinal comparison.
3. Do not alter `changeVersion`, schema migration, numeric/timestamp parsing, or in-memory dirty semantics.
4. Add smoke coverage that saves canonical QSDB, preserves valid dirty round-trip, then mutates one element's dirty token to equivalent noncanonical `+N` and zero-padded forms and requires `Load()` to fail closed.
5. Read back source/test on current `main`; do not dispatch GitHub Actions or claim BricsCAD runtime PASS.
6. Close the claim after source/regression remain visible on current `main`.

## Excluded

- No ProjectSchemaMigrator/changeVersion work; another active lane owns current changeVersion regression.
- No timestamp/numeric/category/map/list/relation changes.
- No BricsCAD adapter/UI or installer/release changes.
