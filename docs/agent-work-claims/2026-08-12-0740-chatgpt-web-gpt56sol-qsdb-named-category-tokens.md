# Work claim — QSDB named category tokens

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12`
- Baseline main SHA: `bac42c5f2339b682b26a041a0c7d87d568e35c08`
- Priority: persistence enum identity canonicality

## Confirmed defect

Current-schema `.qsdb` files serialize `ElementCategory` values as symbolic enum names, but the load path ultimately uses `Enum.TryParse(..., ignoreCase: true)` plus `Enum.IsDefined(...)`. .NET enum parsing accepts numeric strings for defined underlying values, so a file can use a numeric category alias (for example the integer ordinal of `ArchitecturalWall`) and still load as valid even though QS3D itself never writes that representation.

The repository already treats the same pattern as a canonicality defect for drawing-unit metadata: persisted enum identities must be named tokens, not numeric aliases.

## Reserved scope

- `src/QS3D.Core/Persistence/QsdbProjectXmlSchemaValidator.cs`
- focused `QS3D.Core.SmokeTests` regression
- `docs/plans/2026-08-12-qsdb-named-category-tokens.md`
- this claim file

## Non-overlap

- Do not modify `ElementCategory`, rule engine semantics, ProjectElement/ProjectFamily, or native BricsCAD callers.
- Do not change enum casing compatibility: symbolic category names remain case-insensitive.
- Do not change category ordering/values or bump the QSDB schema version.
- No GitHub Actions dispatch or release publication.

## Intended contract

1. Family, quantity-rule, and element `category` attributes must name a defined `ElementCategory` symbol.
2. Numeric aliases are rejected even when their underlying integer is currently defined.
3. Case-insensitive symbolic names remain accepted.
4. Serializer output remains unchanged.

## Closure

- Claim before code and planning before implementation.
- Re-fetch target after planning.
- Add isolated smoke coverage for all three category-bearing persisted surfaces plus symbolic case compatibility.
- Verify ancestry against latest `main` before close.
- Do not claim CI/native runtime PASS unless actually executed.
