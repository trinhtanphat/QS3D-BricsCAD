# Work claim — QSDB named category tokens

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12`
- Baseline main SHA: `bac42c5f2339b682b26a041a0c7d87d568e35c08`
- Priority: persistence enum identity canonicality

## Confirmed defect

Current-schema `.qsdb` files serialized `ElementCategory` values as symbolic enum names, but the load path ultimately used `Enum.TryParse(..., ignoreCase: true)` plus `Enum.IsDefined(...)`. That combination accepts numeric strings for defined underlying values, so a file could use a numeric category alias and still load even though QS3D itself never writes that representation.

## Completed contract

1. Family, quantity-rule, and element `category` attributes must now name a defined `ElementCategory` symbol.
2. Numeric aliases are rejected even when their underlying integer is currently defined.
3. Case-insensitive symbolic names remain accepted.
4. Serializer output and enum values/order remain unchanged.

## Commits

- Claim registration: `09da9010f12c02793de8f5c101684068a9640821`
- Planning: `4266743037326397e9977b250875f66ac7dd06fa`
- Source fix: `4b83dad26f19fe6d564f1497d63a7030e99f7a3a`
- Focused smoke regression source: `bce535e8d684bb95ec34266b3d321610973ae513`

## Validation evidence

- Exact source diff was read back and is limited to the persistence XML validator: one Domain import, three category validation calls, and one named-token helper.
- Source and smoke commits were verified as ancestors of observed `main` `0e4299ccc8a2148a799a2c9227946b83909201ec` with `behind_by: 0`.
- Smoke covers numeric aliases on Family/QuantityRule/ProjectElement plus lower-case valid symbolic tokens.
- Regression source was committed but GitHub Actions were not dispatched in this remote session.
- No CI PASS, build PASS, licensed BricsCAD runtime PASS, or release publication is claimed.

## Released scope

This claim is complete; the persistence validator is released for other agents.
