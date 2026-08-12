# Work claim — Documentation catalog named enum tokens

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T08:02:00+07:00`
- Baseline main SHA observed: `85daf8844c99fbb6d265e28acb80b8ebe2dc00d3`
- Priority: P1 — persisted documentation identity canonicality

## Confirmed defect

`SemanticDocumentationCatalogStore.ReadViews(...)` parses persisted `SemanticViewKind` and `ElementCategory` values with `Enum.TryParse(..., ignoreCase: true)` plus `Enum.IsDefined(...)`. That combination accepts numeric strings for defined underlying enum values, while the catalog serializer always writes symbolic enum names. A hand-edited/current-format catalog can therefore use multiple textual identities for the same persisted view/category semantics.

## Reserved scope

- `src/QS3D.Core/Documentation/SemanticDocumentationCatalogStore.cs`
- focused Core smoke regression under `tests/QS3D.Core.SmokeTests/`
- `docs/plans/2026-08-12-documentation-catalog-named-enum-tokens.md`
- this claim file

## Intended contract

1. Persisted `view kind` must name a defined `SemanticViewKind` symbol.
2. Persisted view `category value` must name a defined `ElementCategory` symbol.
3. Numeric aliases are rejected even when their underlying integer is currently defined.
4. Symbolic names remain case-insensitive for compatibility.
5. Serializer output, enum values/order, catalog schema version, save bounds and planner semantics remain unchanged.

## Non-overlap

- Do not modify `SemanticViewPlanner`, `SemanticSheetPlanner`, catalog save capacities, editor/UI/native CAD surfaces, or licensing/regeneration/XLSX/BOM lanes.
- No GitHub Actions dispatch and no release publication.

## Closure

Claim first, planning before implementation, re-fetch exact store blob before source write, add focused regression for both enum-bearing persisted surfaces plus lower-case symbolic compatibility, verify ancestry against moving `main`, and close without claiming unexecuted CI/runtime PASS.
