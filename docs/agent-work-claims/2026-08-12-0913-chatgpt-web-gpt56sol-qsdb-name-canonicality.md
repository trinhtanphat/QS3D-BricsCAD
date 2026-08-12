# Work claim — QSDB persisted-name canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-qsdb-name-canonicality`
- Registered: `2026-08-12T09:13:00+07:00`
- Baseline main SHA: `c309f009041596cf72f577d6751d4725dfcefe68`
- Priority: P1 deterministic persistence / fail-closed token integrity found during owner-requested `continue all` audit.

## Confirmed defect

QSDB domain constructors/setters canonicalize project, zone, floor and family display names by trimming leading/trailing whitespace before serialization. `QsdbProjectStore.Serialize(...)` therefore emits canonical names. `QsdbProjectXmlSchemaValidator`, however, validates canonical identities (`projectId`, zone/floor/family ids, etc.) but does not validate the corresponding `name` attributes. `QsdbProjectStore.Load()` then feeds those values through `Required(...)`, which trims them, allowing a current-schema file with a persisted name such as `" Level 1 "` to load as `"Level 1"` and be silently rewritten on the next save.

The repository's QSDB persistence has constructed these domain objects through trimming constructors since its initial implementation, so files produced by supported writers already contain canonical display names. This lane only rejects noncanonical persisted representations; it does not alter valid names.

## Reserved scope

- `src/QS3D.Core/Persistence/QsdbProjectXmlSchemaValidator.cs`
- one focused Core smoke source under `tests/QS3D.Core.SmokeTests/`
- this claim file for close-out

## Plan

1. Re-fetch moving `main`, current validator/store/domain contracts and this claim before writes.
2. Require canonical non-empty `name` attributes for project root, zones, floors and families using the existing `ValidateRequiredCanonicalAttribute(...)` helper.
3. Keep case/Unicode/content semantics unchanged; only leading/trailing-whitespace normalization is rejected.
4. Add smoke coverage that saves canonical QSDB, verifies valid round-trip, then independently pads project/zone/floor/family names and requires `Load()` to fail closed.
5. Read back source/test on current `main`; do not dispatch GitHub Actions or claim BricsCAD runtime PASS.
6. Close claim after source/regression remain visible on current `main`.

## Excluded

- No IDs, quantity-rule output/expression/version, metadata values, numeric/timestamp/dirty/changeVersion, migration, or relation changes.
- No BricsCAD adapter/UI or installer/release changes.
