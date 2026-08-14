# Work claim — QSDB Family/element category reference integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-qsdb-family-category-reference`
- Registered: `2026-08-14T22:55:00+07:00`
- Baseline main SHA: `07ec0cf3a4718854fe064b6197f3129d0fab0b16`
- Implementation branch: `agent/chatgpt-web-gpt56sol/qsdb-family-category-reference-20260814`
- Planned integration branch: `integration/chatgpt-web-gpt56sol-qsdb-family-category-reference-20260814`
- Priority: Core P1 persistence / semantic relation integrity found during owner-requested continue-all audit

## Confirmed defect

`ProjectFamilyService.Assign(...)` treats a referenced Family whose `Category` differs from the element category as invalid state that must be repaired before reassignment. However the public `ProjectFamily.Category` setter can still change a Family to another defined `ElementCategory`, and current QSDB persistence validates only that Family/element categories are individually defined plus that `familyId` references an existing Family.

Therefore an element can reference an existing Family of the wrong category and that contradictory semantic relation can pass the current persistence contract. `QsdbProjectXmlSchemaValidator.ValidateElementReferences(...)` likewise checks Family id existence but not Family/element category parity, so an already-corrupt current-schema QSDB can cross the XML semantic boundary without this relation invariant being enforced.

## Reserved scope

Fail closed on mismatched **referenced** Family/element categories at QSDB persistence boundaries while preserving existing domain/category mutation APIs:

- in-memory project validation rejects an element whose non-empty `FamilyId` resolves to a Family with a different `ElementCategory`;
- current-schema XML validation rejects the same mismatch before domain materialization;
- matching Family/element categories continue to round-trip;
- empty/unbound `FamilyId` remains valid;
- existing missing-Family, duplicate-id and category-token validation remains unchanged.

## Expected write surfaces

- `src/QS3D.Core/Persistence/QsdbProjectStore.cs`
- `src/QS3D.Core/Persistence/QsdbProjectXmlSchemaValidator.cs`
- `tests/QS3D.Core.SmokeTests/QsdbCanonicalPersistenceSmoke.cs`
- this claim for coordination and close-out

## Explicit exclusions / concurrency protection

- Do **not** modify `src/QS3D.Core/Domain/ProjectState.cs`, `ProjectFamily`, `ProjectElement`, or `ProjectFamilyService`; the active DrawingPath lane owns `ProjectState.cs`, and this lane does not redesign category mutation semantics.
- No schema-version/migration change, no Family assignment UX/API redesign, no quantity-rule category changes, no metadata/revision-stamp work.
- No overlap with active DrawingPath XML persistability, Core multicore diagnostics, UI/DPI, FieldMerge documentation parity, package-integrity or commercial-signing claims.
- No BricsCAD adapter/native/runtime, LOCAL_ONLY, release workflow or Actions operation.
- No force-push and no manual GitHub Actions dispatch/rerun/cancel.

## Validation plan

- Construct a valid project with a Family and referenced element of the same category; prove normal QSDB round-trip remains valid.
- Mutate the referenced Family to another defined category and prove `Save(...)` fails with `InvalidDataException` without publishing a new QSDB file.
- Tamper a valid current-schema QSDB Family category to another named, defined category and prove `Load(...)` fails with `InvalidDataException` at the persistence semantic boundary.
- Preserve empty/unbound Family behavior and existing missing-reference validation.
- Review exact branch diff/readback, reconcile against refreshed `main`, integrate through the declared integration branch, then observe only automatically triggered CI evidence.
- Do not report managed/native PASS unless actually observed for the exact integrated ancestry.

## Completion condition

The two QSDB persistence boundaries enforce referenced Family/element category parity, focused deterministic regression source is present, the implementation is represented in one reconciled integration landing on current `main`, ancestry/readback is verified, and this claim is closed with exact SHAs and truthful CI evidence.
