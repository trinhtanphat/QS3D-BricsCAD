# Work claim — QSDB duplicate quantity-name read guard

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-gpt56sol-20260812-qsdb-duplicate-quantity-read`
- Registered: `2026-08-12T00:06:00+07:00`
- Last Updated: `2026-08-12T00:17:32+07:00`
- Baseline main SHA: `e22ce35530a78df4a536c7d2bf1eeb908d91b593`
- Priority: persisted quantity ambiguity found during the owner-requested continue-all audit
- Task Key: `PERSISTENCE-QSDB-DUPLICATE-QUANTITY-NAME-READ`
- Implementation PR: `#565`
- Implementation commit on `main`: `eaa0116865773848666697be09187c80a1bfd90e`

## Confirmed defect

Current-schema QSDB structure validated each persisted element quantity name, but the load path materialized quantity entries by calling `ProjectElement.SetQuantity(...)` into the element's case-insensitive quantity dictionary. A file containing two entries such as `Area` and `area` could therefore overwrite the first value with the second before `ValidateProject(...)` ran. The ambiguity was lost by then, so post-materialization validation could not detect the original duplicate persisted state.

This violated the repository fail-closed persistence contract: ambiguous persisted semantic/key data must not be silently normalized or repaired merely to keep loading.

## Implemented scope

`QsdbProjectStore.Load(...)` now resolves each persisted quantity name/value first, checks the element's case-insensitive quantity dictionary for an existing name, and throws `InvalidDataException` before any duplicate can overwrite the previously materialized value. Unique quantity names retain their original values, and the same quantity name remains valid on different semantic elements.

Focused coverage was added to `QsdbCanonicalPersistenceSmoke` for:

- exact duplicate names such as `AreaM2` + `AreaM2`;
- case-only duplicates such as `AreaM2` + `aream2`;
- valid roundtrip of the same quantity name on two different elements with distinct values.

## Surfaces changed

- `src/QS3D.Core/Persistence/QsdbProjectStore.cs`
- `tests/QS3D.Core.SmokeTests/QsdbCanonicalPersistenceSmoke.cs`
- this claim file

## Concurrency / publication notes

- `src/QS3D.Core/Persistence/QsdbProjectXmlSchemaValidator.cs` was intentionally left untouched because concurrent ACTIVE claims reserved that surface.
- Two direct fast-forward publication attempts were correctly rejected after `main` advanced concurrently; no force push or overwrite was used.
- The reviewed two-file implementation was published through PR `#565` and squash-merged server-side after GitHub reported `mergeable=true` and a diff of exactly two files (`+56/-1`).
- The resulting `main` commit is `eaa0116865773848666697be09187c80a1bfd90e`.
- No GitHub Actions workflow/build/release dispatch was performed.
- No BricsCAD adapter/runtime or `LOCAL_ONLY` surface changed, so no new local qualification gate was created.

## Validation evidence

- Re-fetched current `main` before implementation and confirmed both reserved source/test blobs were unchanged from the reviewed snapshot.
- Reviewed PR `#565` unified diff before merge; only the two intended files were present.
- Read back implementation commit `eaa0116865773848666697be09187c80a1bfd90e`; its diff contains the duplicate-name guard plus the exact/case-insensitive regression coverage and no unrelated files.
- Local build/smoke execution was **not** claimed because this connector-only environment does not provide the repository checkout/build runner used by the project.

## Completion

`COMPLETED`: current `main` now fails closed on ambiguous same-element persisted quantity-name duplicates before dictionary overwrite, preserves valid distinct-element quantity identity, and carries focused deterministic regression source with exact implementation evidence above.
