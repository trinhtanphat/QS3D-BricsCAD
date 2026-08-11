# Work claim — QSDB duplicate quantity-name read guard

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-qsdb-duplicate-quantity-read`
- Registered: `2026-08-12T00:06:00+07:00`
- Last Updated: `2026-08-12T00:06:00+07:00`
- Baseline main SHA: `e22ce35530a78df4a536c7d2bf1eeb908d91b593`
- Priority: persisted quantity ambiguity found during the owner-requested continue-all audit
- Task Key: `PERSISTENCE-QSDB-DUPLICATE-QUANTITY-NAME-READ`

## Confirmed defect

Current-schema QSDB structure validates each persisted element quantity name, but the load path materializes quantity entries by calling `ProjectElement.SetQuantity(...)` into the element's case-insensitive quantity dictionary. A file containing two entries such as `Area` and `area` can therefore overwrite the first value with the second before `ValidateProject(...)` runs. The ambiguity is lost by then, so post-materialization validation cannot detect the original duplicate persisted state.

This violates the repository fail-closed persistence contract: ambiguous persisted semantic/key data must not be silently normalized or repaired merely to keep loading.

## Reserved scope

Reject same-element persisted quantity-name duplicates case-insensitively in the QSDB load/materialization path before any dictionary overwrite can occur. Preserve all unique canonical quantity names and values unchanged. The same quantity name remains valid on different semantic elements.

## Expected surfaces

- `src/QS3D.Core/Persistence/QsdbProjectStore.cs`
- one focused Core smoke under `tests/QS3D.Core.SmokeTests/` for duplicate persisted quantity-name load behavior
- this claim file

## Explicit exclusions / concurrency protection

- Do **not** modify `src/QS3D.Core/Persistence/QsdbProjectXmlSchemaValidator.cs`; concurrent ACTIVE claims currently reserve persisted relation/source duplicate validation and primary semantic-ID canonicality on that surface.
- No source-handle/dependency duplicate or canonicality work.
- No quantity-rule engine, quantity UI, reporting, interchange JSON or migration/schema-version changes.
- No BricsCAD adapter/runtime mutation and no new LOCAL_ONLY gate.
- No GitHub Actions dispatch, build/release dispatch or release publication.

## Validation plan

- A current-schema element containing exact duplicate persisted quantity names fails load instead of last-write-wins overwrite.
- A current-schema element containing case-only duplicate persisted quantity names also fails load because quantity identity is case-insensitive.
- A valid element with unique quantities continues to load all values unchanged.
- The same quantity name on two different elements remains valid.
- Re-fetch the exact current source before implementation, review the final merged diff against latest `main`, and read back the resulting commit/source. Do not claim local build/smoke execution unless it is actually run.

## Completion condition

Current `main` rejects ambiguous same-element persisted quantity-name duplicates before overwrite, focused deterministic regression source is present, and this claim is closed `COMPLETED` with exact implementation commit evidence.
