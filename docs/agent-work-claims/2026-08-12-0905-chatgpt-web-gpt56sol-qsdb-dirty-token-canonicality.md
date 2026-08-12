# Work claim — QSDB dirty-token canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-qsdb-dirty-token-canonicality`
- Registered: `2026-08-12T09:05:00+07:00`
- Baseline main SHA: `8f0b933ac03a737ec6dbc24d9b69aec7fd7677bd`
- Regression commit: `7b4f3ea887af217d0d1b991f86062abd708a531b`
- Completed source commit: `3dc131142cca0711b77415bccf564197895cfc4b`
- Readback main SHA before close-out: `a41b81ea8b1251ad51cb5606bb645fc40f3b3c77`
- Priority: P1 deterministic persistence / fail-closed token integrity found during owner-requested `continue all` audit.

## Confirmed defect

`QsdbProjectStore.Serialize(...)` emits element dirty flags as `((int)x.Dirty).ToString(CultureInfo.InvariantCulture)`. Historical source at `f497819ad7de4178e42f25c070c38ac77b850412` already used that exact canonical representation when dirty-state persistence was present. The previous `Dirty(...)` loader used `int.TryParse(..., NumberStyles.Integer, ...)`, which accepted semantically equivalent noncanonical tokens such as `+15`, `015`, or padded integer text and converted them to the same enum value. A later save silently rewrote the token.

Legacy migration seeds a missing v1 dirty attribute with canonical invariant integer text, so enforcing canonical persisted tokens does not reject values emitted or synthesized by supported repository writers/migrations.

## Implemented contract

1. Existing integer parse, non-negative check and `ElementDirtyFlags.All` bitmask validation remain unchanged.
2. After validation, `Dirty(...)` derives `raw.ToString(CultureInfo.InvariantCulture)`.
3. The original persisted value must exactly match that token with `StringComparison.Ordinal`; otherwise `Load()` fails closed with `InvalidDataException`.
4. No `changeVersion`, schema migration, timestamp/numeric/category/list/map behavior changed.
5. Focused smoke coverage saves a canonical QSDB and verifies `ElementDirtyFlags.All` round-trip, then independently rewrites the token to `+15` and `015` and requires `Load()` to reject each.

## Verification

- Current-main source readback confirmed range/bitmask validation followed by exact canonical integer comparison.
- Current-main smoke readback confirmed canonical round-trip plus signed and zero-padded token cases.
- `3dc131142cca0711b77415bccf564197895cfc4b...main` compared as `ahead` with the source commit as merge base; six later concurrent commits touched unrelated Family/Recognition/docs/smoke files.
- The smoke source is committed but was not executed from this remote connector session. Full Core smoke execution/build and GitHub Actions were not run; no PASS is fabricated.
- This is Core persistence work and makes no licensed BricsCAD runtime claim.

## Excluded

- No ProjectSchemaMigrator/changeVersion work; concurrent ownership was respected.
- No timestamp/numeric/category/map/list/relation changes beyond preserving already-merged contracts.
- No BricsCAD adapter/UI or installer/release changes.
