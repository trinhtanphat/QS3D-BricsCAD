# Work claim — QSDB changeVersion token canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-qsdb-changeversion-canonicality`
- Registered: `2026-08-12T09:40:00+07:00`
- Baseline main SHA: `f1f8e8e2e647db4d67ea9da7703e3cbc289ec98f`
- Priority: P1 deterministic persistence / fail-closed token integrity found during owner-requested `continue all` audit.

## Confirmed defect

The original change-version persistence commit `ba50b0c6655eeea0e0fb69b5a3cf5dade9770796` writes `project.ChangeVersion.ToString(CultureInfo.InvariantCulture)`. Current migration also synthesizes missing legacy changeVersion as canonical `0`. The loader's `ChangeVersion(...)` rejects blank, signed/negative/malformed/out-of-range values, but `long.TryParse(..., NumberStyles.None, ...)` still accepts semantically equivalent zero-padded tokens such as `01`. A current-schema QSDB can therefore load `changeVersion="01"` as `1` and silently rewrite it as `1` on the next save.

The completed missing-current-changeVersion lane owns attribute presence/migration only and is already closed; this lane does not modify `ProjectSchemaMigrator`.

## Reserved scope

- `src/QS3D.Core/Persistence/QsdbProjectStore.cs`
- one focused Core smoke source under `tests/QS3D.Core.SmokeTests/`
- this claim file for close-out

## Plan

1. Re-fetch moving `main`, current store and this claim before writes.
2. Preserve existing parse/non-negative/range behavior, then require the original token to exactly match `result.ToString(InvariantCulture)` using ordinal comparison.
3. Preserve legacy migration/default behavior and valid current-schema round-trip.
4. Add smoke coverage that writes canonical QSDB, verifies changeVersion round-trip, mutates the persisted token to an equivalent zero-padded form, and requires `Load()` to fail closed.
5. Read back source/test on current `main`; no GitHub Actions or BricsCAD runtime PASS.
6. Close claim only after source/regression remain visible on current `main`.

## Excluded

- No ProjectSchemaMigrator or missing-attribute behavior changes.
- No timestamp/numeric/dirty/category/name/rule-text or adapter/UI changes.
- No installer/release work.
