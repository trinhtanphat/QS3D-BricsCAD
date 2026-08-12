# Work claim — QSDB persisted-name canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-qsdb-name-canonicality`
- Registered: `2026-08-12T09:13:00+07:00`
- Baseline main SHA: `c309f009041596cf72f577d6751d4725dfcefe68`
- Regression commit: `fb05a821caaae93c380c8f44beb14608af9dec35`
- Completed source commit: `228a37acc58ddd376e253fc3c8994e1606576a37`
- Readback main SHA before close-out: `870811fb578f6afa7231fd0b9636139544cdd64f`
- Priority: P1 deterministic persistence / fail-closed token integrity found during owner-requested `continue all` audit.

## Confirmed defect

QSDB domain constructors/setters canonicalize project, zone, floor and family display names by trimming leading/trailing whitespace before serialization. `QsdbProjectStore.Serialize(...)` therefore emits canonical names. The previous `QsdbProjectXmlSchemaValidator` validated canonical identities but did not validate these `name` attributes. `QsdbProjectStore.Load()` feeds them through `Required(...)`, which trims, so current-schema persisted names such as `" Level 1 "` could load as `"Level 1"` and be silently rewritten later.

The repository's QSDB persistence has constructed these domain objects through trimming constructors since its initial implementation, so files produced by supported writers already contain canonical display names.

## Implemented contract

1. Root project `name` is now required and canonical through `ValidateRequiredCanonicalAttribute(...)`.
2. Zone, floor and family `name` attributes use the same non-empty/no-leading-or-trailing-whitespace validation.
3. Validation occurs at the XML schema boundary before `Required(...).Trim()` materializes domain objects.
4. Case, Unicode and internal whitespace semantics are unchanged.
5. Focused smoke coverage preserves canonical project/zone/floor/family round-trip and independently pads each of the four persisted name surfaces, requiring `Load()` to reject each.

## Verification

- Current-main validator readback confirmed all four canonical-name checks.
- Current-main smoke readback confirmed canonical round-trip and project/zone/floor/family padded-name cases.
- `228a37acc58ddd376e253fc3c8994e1606576a37...main` compared as `ahead` with the source commit as merge base; later concurrent changes touched unrelated diagnostics/rebar/revisions/docs/tests.
- Smoke source was committed but not executed from this remote connector session. Full Core smoke execution/build and GitHub Actions were not run; no PASS is fabricated.
- This is Core persistence work and makes no licensed BricsCAD runtime claim.

## Excluded

- No IDs, QuantityRule output/expression/version, metadata values, numeric/timestamp/dirty/changeVersion, migration, or relation changes.
- No BricsCAD adapter/UI or installer/release changes.
