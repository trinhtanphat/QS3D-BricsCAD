# Work claim — Documentation catalog version token canonicality

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T08:14:00+07:00`
- Baseline main SHA observed: `9c0a4ffe403b98167f790648aaec51333bdb7498`
- Priority: P1 — persisted documentation schema identity canonicality

## Confirmed defect

`SemanticDocumentationCatalogStore.Load(...)` parses the catalog `version` through `int.TryParse(..., NumberStyles.Integer, ...)`. This accepts alternate textual representations such as a leading sign and surrounding whitespace, while ordinary integer parsing also accepts leading-zero aliases. The catalog serializer always emits the single canonical token `1`, so the loader currently accepts multiple textual identities for the same persisted format version.

## Reserved scope

- `src/QS3D.Core/Documentation/SemanticDocumentationCatalogStore.cs` version parser only
- focused Core smoke regression under `tests/QS3D.Core.SmokeTests/`
- `docs/plans/2026-08-12-documentation-catalog-version-token.md`
- this claim file

## Contract

1. Catalog version tokens must be unsigned invariant decimal integers in exact round-trip form.
2. Canonical token `1` remains accepted.
3. Aliases such as `01`, `+1`, and ` 1 ` fail closed.
4. No catalog version bump, serializer change, enum-token change, save-bound change, or planner behavior change.

## Non-overlap

- Do not modify view/category enum-token policy beyond the already completed lane.
- Do not modify documentation editor/planners, native CAD/UI, licensing, regeneration, XLSX/BOM/interchange lanes.
- No GitHub Actions dispatch or release publication.

## Closure

Claim before source, planning before implementation, exact blob re-fetch, focused regression against the real saved payload, ancestry verification on moving `main`, truthful closure without unexecuted CI/runtime PASS claims.
