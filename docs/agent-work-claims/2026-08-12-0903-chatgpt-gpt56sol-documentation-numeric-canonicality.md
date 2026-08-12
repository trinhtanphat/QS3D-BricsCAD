# Work claim — Documentation Catalog numeric lexical canonicality

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-gpt56sol-20260812-documentation-numeric-canonicality`
- Registered: `2026-08-12T09:03:00+07:00`
- Completed: `2026-08-12T09:13:00+07:00`
- Baseline main SHA: `4601218af86e01f1909cc7bf688bc87315e59e88`
- Claim commit: `f67d40d9e56214c1b3af94078b560dbec0ad0a28`
- Reviewed source commit: `763cc443133df3dba5d36fed8a62c351f5f6b190`
- Reviewed smoke commit: `f7cb1e1eb830fb7a595ba2ef633b4ac506146df7`
- Synchronized combined commit: `b2bacca709834d237d517a78dc91b8206587976b`
- Replaced stale PR: `#675` (closed unmerged)
- Integrated PR: `#678`
- Main integration commit: `627cd87ed4b5191f6664c3de6ea56491e18a32cd`
- Priority: deterministic persisted-format integrity during owner-requested continue-all audit
- Task Key: `CORE-DOCUMENTATION-CATALOG-NUMERIC-CANONICALITY`

## Confirmed defect

`SemanticDocumentationCatalogStore.Serialize(...)` emits persisted sheet dimensions and placement coordinates/sizes with invariant round-trip (`"R"`) numeric formatting, while the prior `Load(...)` path accepted semantically equivalent finite `double` spellings and trimmed numeric whitespace before parsing. Values such as `1000.0` or ` 500 ` could therefore be normalized silently on a later save.

## Completed scope

- Sheet dimensions and placement coordinates/sizes now preserve their raw persisted lexical token until numeric validation.
- The shared numeric parser requires the raw token to exactly equal the invariant round-trip representation emitted by `Number(...)`.
- Canonical valid values retain existing finite/range/schema/count and semantic validation.
- String/id handling, collection-order canonicality, UI/native BricsCAD and release/update surfaces were not changed.

## Validation performed

- Focused smoke creates a canonical catalog through `SemanticDocumentationCatalogStore.Save(...)`, confirms it remains loadable, then proves `1000.0`, `10.0` and whitespace-padded numeric attributes fail closed.
- Reviewed the source diff and smoke diff directly.
- Compared concurrent `main` changes from the claim and synchronization bases; no concurrent commit touched `SemanticDocumentationCatalogStore.cs` or the new smoke path.
- PR #675 became stale during rapid `main` churn and was closed unmerged; the exact reviewed blobs were rebuilt non-force on a newer `main` base and integrated through PR #678.
- Re-read source and smoke from `main` after integration and confirmed both are present unchanged.
- No GitHub Actions/build/release dispatch was performed.
- No local .NET build or BricsCAD V25/V26 runtime PASS is claimed from this remote session.

## Completion condition

Completed. Documentation Catalog persisted numeric attributes now fail closed on noncanonical lexical spellings, focused regression coverage is on current `main`, and the reservation is released.
