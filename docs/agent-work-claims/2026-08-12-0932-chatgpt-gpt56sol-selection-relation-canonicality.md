# Work claim — Semantic Selection relation-ID canonicality

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-selection-relation-canonicality`
- Registered: `2026-08-12T09:32:00+07:00`
- Baseline main SHA: `3990285a4fa98b5d1521f0c52eb5feaa43fe933e`
- Priority: fail-closed semantic inspection integrity during owner-requested continue-all audit
- Task Key: `CORE-SELECTION-RELATION-ID-CANONICALITY`

## Confirmed defect

`ProjectElement.FamilyId`, `FloorId`, and `ZoneId` are public mutable relation fields, so runtime state can contain whitespace-padded nonblank IDs after construction. `SemanticSelectionInspector.ValidateSemanticReferences(...)` and `InspectReference(...)` currently trim those values before lookup/output. A malformed relation such as `" FAMILY-1 "` can therefore be accepted and surfaced as canonical `"FAMILY-1"` instead of being reported as invalid state.

This conflicts with the repository's fail-closed semantic-reference integrity pattern and with the recently hardened reporting boundary, which rejects whitespace-padded nonblank relation IDs instead of silently normalizing them.

## Reserved scope

- `src/QS3D.Core/Selection/SemanticSelectionInspector.cs`
- one focused Core smoke file for Selection relation-ID canonicality
- this claim file for close-out

## Contract

- preserve existing blank-reference allowance (`null`, empty, or whitespace-only remain absent references);
- require every nonblank selected element `FamilyId` / `FloorId` / `ZoneId` to already equal its trimmed canonical spelling before lookup or inspection output;
- fail closed before returning a `SemanticSelectionInspection` when a selected relation ID is whitespace-padded;
- preserve existing missing-reference, duplicate-project-identity, category-mismatch, property/quantity and ownership-filter behavior;
- do not broaden into bulk-edit mutation, UI/native BricsCAD, reporting, or general ProjectElement setter policy.

## Validation plan

Add focused ModuleInitializer smoke coverage proving a canonical selected element remains inspectable, while runtime-mutated whitespace-padded Family/Floor/Zone relation IDs each fail closed instead of being normalized in the inspection result.

No GitHub Actions/build/release dispatch and no BricsCAD V25/V26 runtime PASS claim from this remote lane.
