# Work claim — Documentation catalog named enum tokens

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T08:02:00+07:00`
- Completed: `2026-08-12T08:13:00+07:00`
- Baseline main SHA observed: `85daf8844c99fbb6d265e28acb80b8ebe2dc00d3`
- Priority: P1 — persisted documentation identity canonicality

## Confirmed defect

`SemanticDocumentationCatalogStore.ReadViews(...)` parsed persisted `SemanticViewKind` and `ElementCategory` values with `Enum.TryParse(..., ignoreCase: true)` plus `Enum.IsDefined(...)`. That combination accepts numeric strings for defined underlying enum values, while the catalog serializer always writes symbolic enum names. A hand-edited/current-format catalog could therefore use multiple textual identities for the same persisted view/category semantics.

## Completed contract

1. Persisted `view kind` must name a defined `SemanticViewKind` symbol.
2. Persisted view `category value` must name a defined `ElementCategory` symbol.
3. Numeric aliases are rejected even when their underlying integer is currently defined.
4. Symbolic names remain case-insensitive for compatibility.
5. Serializer output, enum values/order, catalog schema version, save bounds and planner semantics remain unchanged.

## Commits

- Claim registration: `62e4b2c13df0f331756067457a1e7069994dca66`
- Planning: `94e3f4dc41177cb4b6671e010e36510861ad3bab`
- Source fix: `0e66b093f8353963dcf6b47e504ba505e8fbd0d2`
- Focused smoke regression source: `3ac7237700816fc5e4b4de2ca7cf299a88225237`

## Validation evidence

- The source blob was re-fetched after planning and remained `bd2dd71de676afcedeaa41d84170e626c85b4f07` before the write.
- Exact source diff was read back: only the two enum parse sites and one named-token helper changed (`+12/-6`).
- Source commit and smoke commit were verified as ancestors of observed `main` `7682538f2ef6875ca09c1ee52b356e5db10b435b` with `behind_by: 0`.
- Concurrent commits after the source change did not modify `SemanticDocumentationCatalogStore.cs`.
- Smoke source covers numeric `SemanticViewKind`, numeric `ElementCategory`, and lower-case symbolic compatibility using a payload produced by the real catalog `Save(...)` path.
- GitHub Actions were not dispatched; executable smoke/build PASS and licensed BricsCAD runtime PASS are not claimed.

## Released scope

This lane is complete. `SemanticDocumentationCatalogStore.cs` is released for other agents; the excluded planner/editor/UI/native/licensing/regeneration/XLSX/BOM scopes were not modified.
