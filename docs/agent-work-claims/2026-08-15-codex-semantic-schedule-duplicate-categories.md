# Work claim — Semantic Schedule duplicate category integrity

- Status: `ACTIVE`
- Agent: `Codex /root/audit_docs_next`
- Registered: `2026-08-15T11:20:09+07:00`
- Baseline main SHA: `44e3c9aac8ac8cffade4b10021c5b5b933e584ae`
- Issue: `#77`
- Priority: remote-safe Core documentation correctness

## Confirmed defect

`SemanticScheduleCatalog.Normalize(...)` begins category normalization with `raw.Categories.Distinct()`. `Load(...)` validates the allowed XML node/attribute shape, then `ReadDefinition(...)` feeds every persisted `<category>` node through that normalization.

A malformed v1 payload containing two identical canonical nodes such as `<categories><category value="Beam"/><category value="Beam"/></categories>` therefore passes schema and enum-name checks, is silently collapsed to one Beam category, and loads as valid. Re-saving changes the payload by dropping the duplicate. This is lossy acceptance of noncanonical stored metadata, unlike the catalog's explicit duplicate rejection for include/exclude IDs.

## Reserved scope

- `src/QS3D.Core/Documentation/SemanticScheduleCatalog.cs`: reject duplicate category values explicitly during normalization while preserving defined-enum validation, the existing 5,000-entry raw category snapshot bound, and deterministic ordering.
- `tests/QS3D.Core.SmokeTests/SemanticScheduleCatalogSmoke.cs`: extend existing category-canonicality coverage for duplicate in-memory filters, duplicate persisted canonical nodes, and a valid multi-category deterministic round-trip.
- `scripts/preflight-semantic-schedule-catalog.py`: extend the existing focused static gate only.
- This claim file for registration and handoff.

## Intended contract

- Every category value must be a defined `ElementCategory` and occur at most once in a normalized Semantic Schedule definition.
- Duplicate categories must fail closed rather than be silently canonicalized away on Save, Build, or Load.
- Valid distinct multi-category filters retain deterministic enum-name ordering and exact round-trip behavior.
- Preserve the 128-schedule, 5,000-ID/category, 32-column, payload-size, DTD/schema, include/exclude, template, zero-match, and non-mutating rendering contracts.

## Explicit exclusions

- No changes to `SemanticDocumentationCatalogStore`, `SemanticViewPlanner`, schedule Save enumeration bounds, include/exclude semantics, catalog version/schema/text grammar beyond duplicate category identity, Health, or documentation-table rendering.
- No BricsCAD V25/V26, native Table, UI, runtime, LOCAL runner/probe, private-data, workflow, GitHub Actions, release, signing, or packaging work.
- Broad issue `#77` remains open.

## Validation plan

- Run the extended focused Semantic Schedule catalog gate plus neighboring Schedule/Documentation gates.
- Build `QS3D.Core` and `QS3D.Core.SmokeTests` in Release and run the full deterministic Core smoke suite.
- Run repository and aggregate remote-safe preflights; report independent blockers without expanding scope.
- Re-fetch `origin/main` before push and final handoff, preserve concurrent work, and stop before merge.
