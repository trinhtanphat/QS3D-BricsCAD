# Work claim — Documentation catalog version token canonicality

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T08:14:00+07:00`
- Completed: `2026-08-12T08:19:00+07:00`
- Baseline main SHA observed: `9c0a4ffe403b98167f790648aaec51333bdb7498`
- Priority: P1 — persisted documentation schema identity canonicality

## Confirmed defect

`SemanticDocumentationCatalogStore.Load(...)` parsed the catalog `version` through `int.TryParse(..., NumberStyles.Integer, ...)`. This accepted alternate textual representations such as a leading sign and surrounding whitespace, while ordinary integer parsing also accepted leading-zero aliases. The catalog serializer always emits the single canonical token `1`, so the loader accepted multiple textual identities for the same persisted format version.

## Completed contract

1. Catalog version tokens are now parsed with `NumberStyles.None` and must exactly match invariant decimal round-trip text.
2. Canonical token `1` remains accepted.
3. Aliases `01`, `+1`, and ` 1 ` fail closed.
4. No catalog version bump, serializer change, enum-token change, save-bound change, or planner behavior change was made.

## Commits

- Claim registration: `90689492638e866e86e8f651b69e77be81110475`
- Planning: `af09b67566dfe30d035c3fffe4aa0445eeaf52b2`
- Source fix: `08115b8629e2fae91e37b9c1011534589556a77f`
- Focused smoke regression source: `96d891de8298812768e39be3b68f2c1a78013344`

## Validation evidence

- Post-plan store blob was re-fetched as `14f3b70aa13b0881c40549aeb4f109fdc5fbcc42` before the source write.
- Exact source diff was read back and changes only `Integer(...)` (`+3/-1`).
- The first smoke-file create hit a GitHub `409` because `main` advanced concurrently; no false commit was reported. HEAD and source ancestry were refreshed and the target path was confirmed absent before retry.
- Source and smoke commits were verified as ancestors of observed `main` `91ae3960cc33b584719082cb451f03500cf1d769` with `behind_by: 0`.
- Concurrent commits after the source fix did not modify `SemanticDocumentationCatalogStore.cs`.
- Smoke source uses a payload produced by the real `Save(...)` path and covers canonical `1` plus `01`, `+1`, and surrounding-whitespace aliases.
- GitHub Actions were not dispatched; executable smoke/build PASS and licensed BricsCAD runtime PASS are not claimed.

## Released scope

This lane is complete; the catalog store version parser is released for other agents. Excluded editor/planner/native/licensing/regeneration/XLSX/BOM/interchange scopes were not modified.
