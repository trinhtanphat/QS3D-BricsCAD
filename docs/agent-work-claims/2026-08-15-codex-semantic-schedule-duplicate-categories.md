# Work claim — Semantic Schedule duplicate category integrity

- Status: `COMPLETED`
- Agent: `Codex /root/audit_docs_next`
- Registered: `2026-08-15T11:20:09+07:00`
- Completed: `2026-08-15T11:46:44+07:00`
- Baseline main SHA: `5a13195e2b49a64c5b2d728bf4af668d1b9bff88`
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

## Completion evidence

- The claim was published and merged first through PR `#1581` at `0bef69ca3f8312cf891956cb7d7fa2b2c1d02e1a` before source, smoke, or gate edits began.
- Implementation commit `3afa7f1fd341d9795caff9b999f19ac4a74b65a5` was reviewed through PR `#1587` and landed on the integration branch as `85b3138280565208f125974d1f05cd053df54432`.
- Integration PR `#1597` then carried that implementation to exact main SHA `e2dbb1e03748047f69a556240f8f85b2e7ccc17e`.
- `SemanticScheduleCatalog.Normalize(...)` now validates every raw category for defined enum membership and duplicate identity before deterministically ordering the accepted distinct values; the existing 5,000-entry raw category snapshot bound is unchanged.
- The extended existing smoke covers in-memory duplicate Save/Build rejection without project mutation, duplicate persisted canonical nodes failing Load, and stable deterministic round-trip of valid Beam/Column filters. The focused static gate pins those contracts.
- Root independently validated exact merged main `e2dbb1e03748047f69a556240f8f85b2e7ccc17e`: the focused Semantic Schedule gate passed; `QS3D.Core` and `QS3D.Core.SmokeTests` Release builds completed with `0` warnings and `0` errors; full Core smoke reported `ALL PASS`; the V25 installed-reference build completed with `0` warnings and `0` errors; and aggregate remote-safe validation passed `824/824` gates.
- No BricsCAD runtime, UI, native Table, LOCAL/private-data, workflow, GitHub Actions, release, signing, or packaging operation was performed by this lane. Broad issue `#77` remains open.
