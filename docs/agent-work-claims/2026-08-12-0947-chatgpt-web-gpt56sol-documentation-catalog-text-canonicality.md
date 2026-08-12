# Agent Work Claim — Documentation Catalog Text Canonicality

- Status: `COMPLETED`
- Owner: ChatGPT web / GPT-5.6 Sol
- Started: 2026-08-12 09:47 +07:00
- Completed: 2026-08-12 09:52 +07:00
- Start commit observed: `4fd253b56a62576f9c9f7f99fe4ccf50fd847a1e`
- Claim commit: `9b19848285d4d8f5162eef8bc0ed631f8dc2b7aa`
- Fix commit: `dd60c1836197548827a3f653bd6143b51c10a32e`
- Regression commit: `9479e0ea6944e5f018431c7ec0634912a13aef8c`
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

`Save(...)` validates definitions through `SemanticViewPlanner` / `SemanticSheetPlanner`, whose required/optional ID and name helpers return trimmed plan values without mutating the original definitions. `SemanticDocumentationCatalogStore.SerializeView(...)` and `SerializeSheet(...)` then serialized those original raw definitions, including raw filter/placement IDs. A whitespace-padded but otherwise valid definition could therefore pass semantic validation and be persisted with noncanonical text.

On Load, the store's XML `Required(...)` and `Optional(...)` helpers called `Trim()` and returned the trimmed value. Therefore a whitespace-padded persisted attribute was silently normalized rather than rejected, so persisted bytes and loaded semantic identity were not one-to-one.

This is distinct from the completed enum-token lane, which intentionally preserves case-insensitive symbolic enum names, and from numeric lexical canonicality.

## Implemented contract

- Save serializes canonical trimmed required text for view/sheet IDs, names, filter IDs and placement view references.
- Save serializes optional floor/zone/title-block text as either the canonical trimmed value or the canonical empty string.
- View/sheet/filter/placement tie-break sorting now uses the same canonical text representation so padded-but-equivalent input cannot alter serialized ordering.
- Load `Required(...)` rejects whitespace-padded persisted text instead of trimming it.
- Load `Optional(...)` accepts the writer's empty-string representation as `null`, but rejects whitespace-only or padded non-empty text.
- Symbolic enum tokens remain case-insensitive after the required-text canonicality check; numeric token behavior is unchanged.

## Regression coverage

`SemanticDocumentationCatalogStoreSmoke` now includes:

1. `WriterCanonicalizesTextTokens` — saves a real view/sheet definition with padded view ID/name/floor/include ID, padded sheet ID/number/name/title block, a whitespace-only optional zone, and a padded placement view reference. The persisted XML must contain none of the padded tokens, and the same payload must Load back to canonical identities including `zoneId == null`.
2. `PaddedPersistedTextFailsClosed` — creates a canonical payload through the real Save path, tampers only the persisted view `id` to add surrounding spaces, and requires Load to throw `InvalidDataException` rather than silently trimming it.

## Validation

- Re-read live `main`, this claim, the store and existing smoke after claim registration. The two commits that landed after the claim touched only release-preflight/Grid smoke surfaces.
- Source fix `dd60c1836197548827a3f653bd6143b51c10a32e` remained on `main`; the next observed commit was an unrelated Quantity Revision canonicality change whose parent was the source fix.
- Regression `9479e0ea6944e5f018431c7ec0634912a13aef8c` was confirmed as an ancestor of live `main` `e9454e2566dfaabf00a6389c3f219ef46fe3f683` with `behind_by: 0`.
- The three commits after the regression touched XLSX Quantity/ED2 smoke/registration and a Zone Assign claim, not either Documentation Catalog target file.
- No GitHub Actions were manually dispatched.
- No local .NET or BricsCAD runtime PASS is claimed in this remote-source lane.

## Overlap note

Recent Documentation Catalog history was checked immediately before registration. Completed lanes covered named enum tokens, version/root sections, numeric lexical tokens, save bounds, and readonly catalogs. No active/recent claim for text whitespace canonicality was found. Concurrent work observed during this lane stayed outside the reserved Documentation Catalog source/smoke paths.
