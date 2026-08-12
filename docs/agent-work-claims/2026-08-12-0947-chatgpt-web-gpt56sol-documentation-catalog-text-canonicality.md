# Agent Work Claim — Documentation Catalog Text Canonicality

- Status: `ACTIVE`
- Owner: ChatGPT web / GPT-5.6 Sol
- Started: 2026-08-12 09:47 +07:00
- Start commit observed: `4fd253b56a62576f9c9f7f99fe4ccf50fd847a1e`
- Priority: P1 — persisted documentation identity canonicality

## Purpose

Make Documentation Catalog text attributes use one canonical persisted representation instead of allowing Save/Load to round-trip through silent whitespace trimming.

## Allowed scope

- `src/QS3D.Core/Documentation/SemanticDocumentationCatalogStore.cs`
- focused `tests/QS3D.Core.SmokeTests/SemanticDocumentationCatalogStoreSmoke.cs` regression coverage
- this claim file

## Excluded scope

- `SemanticViewPlanner` / `SemanticSheetPlanner` public normalization semantics
- enum symbolic-name case-insensitive compatibility established by the completed named-enum-token lane
- numeric token canonicality already fixed by PR #678
- catalog XML schema/version/count limits
- native BricsCAD/UI/reporting/quantity/licensing/release work
- BricsCAD runtime qualification

## Proven defect

`Save(...)` validates definitions through `SemanticViewPlanner` / `SemanticSheetPlanner`, whose required/optional ID and name helpers return trimmed plan values without mutating the original definitions. `SemanticDocumentationCatalogStore.SerializeView(...)` and `SerializeSheet(...)` then serialize those original raw definitions, including raw filter/placement IDs. A whitespace-padded but otherwise valid definition can therefore pass semantic validation and be persisted with noncanonical text.

On Load, the store's XML `Required(...)` and `Optional(...)` helpers currently call `Trim()` and return the trimmed value. Therefore a whitespace-padded persisted attribute is silently normalized rather than rejected, so persisted bytes and the loaded semantic identity are not one-to-one.

This is distinct from the completed enum-token lane, which intentionally preserves case-insensitive symbolic enum names, and from numeric lexical canonicality.

## Contract

- Save must emit canonical trimmed required text and canonical empty-or-trimmed optional text for view/sheet identity/name/reference/filter attributes.
- Canonical sorting must use the canonical text values so equivalent padded input cannot change serialized ordering.
- Load must reject whitespace-padded or whitespace-only optional persisted text instead of silently trimming it.
- Empty optional attributes emitted by the writer remain valid and load as `null`.
- Lower-case symbolic enum names remain accepted as established by the prior compatibility contract.
- Add focused smoke coverage proving writer canonicalization and fail-closed loading of a padded persisted identity token.

## Overlap note

Recent Documentation Catalog history was checked immediately before registration. Completed lanes cover named enum tokens, version/root sections, numeric lexical tokens, save bounds, and readonly catalogs. No active/recent claim for text whitespace canonicality was found. Re-read latest `main`, this claim, source and smoke immediately after registration before implementation.
