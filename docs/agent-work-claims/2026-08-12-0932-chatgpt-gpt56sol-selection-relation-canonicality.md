# Work claim — Semantic Selection relation-ID canonicality

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-gpt56sol-20260812-selection-relation-canonicality`
- Registered: `2026-08-12T09:32:00+07:00`
- Completed: `2026-08-12T09:38:00+07:00`
- Baseline main SHA: `3990285a4fa98b5d1521f0c52eb5feaa43fe933e`
- Claim commit: `9e25bd856f5cd9caf981f816cb1f6902cd32c48c`
- Branch source commit: `ace4e544524b1e3f85136843bd592e3280427472`
- Branch smoke commit: `02bbbddc4bcdc09ab3710685a05d8553113a470f`
- Pull request: `#702`
- Main integration commit: `661cc8400397aeb74a2695ffec69bb49bab33f93`
- Priority: fail-closed semantic inspection integrity during owner-requested continue-all audit
- Task Key: `CORE-SELECTION-RELATION-ID-CANONICALITY`

## Confirmed defect

`ProjectElement.FamilyId`, `FloorId`, and `ZoneId` are public mutable relation fields, so runtime state can contain whitespace-padded nonblank IDs after construction. The prior `SemanticSelectionInspector.ValidateSemanticReferences(...)` and `InspectReference(...)` paths trimmed those values before lookup/output. A malformed relation such as `" FAM-1 "` could therefore be accepted and surfaced as canonical `"FAM-1"` instead of being reported as invalid state.

## Completed scope

- `SemanticSelectionInspector` now validates raw nonblank Family/Floor/Zone relation IDs against their trimmed form before semantic lookup.
- Whitespace-padded nonblank relation IDs fail closed before an inspection result is returned.
- Null/empty/whitespace-only relation references remain allowed as absent references.
- Existing missing-reference, duplicate-project-identity, family/category mismatch, property/quantity and ownership-filter behavior remains intact.
- The concurrent Selection inspector input-freshness guard was preserved during integration.

## Validation performed

- Focused ModuleInitializer smoke verifies canonical Family/Floor/Zone values remain inspectable.
- The smoke mutates each public relation setter to a whitespace-padded nonblank ID and verifies inspection rejects it.
- The smoke also verifies whitespace-only FamilyId remains an allowed blank reference.
- Reviewed source and smoke diffs directly before merge.
- A concurrent completed input-freshness lane changed three earlier lines in `SemanticSelectionInspector.cs`; its diff was reviewed and shown to be independent of the relation-canonicality hunk.
- PR #702 was squash-merged with expected head `02bbbddc4bcdc09ab3710685a05d8553113a470f`.
- Re-read integrated source from `main` and confirmed both the concurrent `ChangeVersion` freshness guard and the new relation canonicality helper are present together.
- Re-read the focused smoke from `main` and confirmed it is present unchanged.
- No GitHub Actions/build/release dispatch was performed.
- No local .NET build or BricsCAD V25/V26 runtime PASS is claimed from this remote session.

## Completion condition

Completed. Semantic Selection inspection no longer silently normalizes whitespace-padded nonblank relation IDs, focused regression coverage is integrated on `main`, concurrent freshness protection is preserved, and the reservation is released.
