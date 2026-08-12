# QSDB Schema Token Canonicality Plan

## Goal

Make the persisted `schema` attribute a single canonical unsigned invariant-decimal token before migration dispatch.

## Implementation

1. Keep `ReadSchema(...)` as the single schema parser.
2. Parse with `NumberStyles.None` so whitespace/signs are rejected.
3. Require the original token to equal `schema.ToString(CultureInfo.InvariantCulture)` so leading-zero aliases are rejected.
4. Preserve existing unsupported/newer-version errors and v1→v2→v3 migration logic.
5. Add isolated smoke coverage for canonical current/legacy tokens and noncanonical `03`, `+3`, ` 3 ` aliases.

## Safety

No migration payload changes, no version bump, no changes to changeVersion/category/audit semantics, and no Actions/release dispatch.
