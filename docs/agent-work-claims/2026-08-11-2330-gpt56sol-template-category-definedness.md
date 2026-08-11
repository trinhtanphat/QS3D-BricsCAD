# Work claim — template category definedness

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-template-category-definedness-20260811-2330`
- Registered: `2026-08-11T23:30:18+07:00`
- Baseline main SHA: `5364d543ce9115b24f54b7727ea3b3797a14e701`
- Priority: deterministic malformed-template boundary defect found during owner-requested `continue all` review

## Reserved scope

Require persisted template family/rule category text to parse to a **defined** `ElementCategory`, not merely a numeric enum value accepted by `Enum.TryParse`.

## Expected surfaces

- `src/QS3D.Core/Templates/TemplateProfileStore.cs`
- `tests/QS3D.Core.SmokeTests/WorkflowPersistenceSmoke.cs`
- this claim file for close-out metadata

## Excluded scope

- No template Name changes (the prior name-integrity lane is completed).
- No native template commands/UI, BricsCAD V25 runtime qualification, family merge policy, quantity rule expression semantics, recognition engine changes or persistence atomicity redesign.
- No GitHub Actions dispatch/re-run and no LOCAL_PASS claim.

## Defect evidence

`TemplateProfileStore.Load` currently checks persisted family/rule categories with `Enum.TryParse` only. Numeric text such as `"999"` therefore passes the parser even though `ElementCategory` does not define that value. The hardened `ProjectFamily` / `QuantityRule` constructors then reject it later with `ArgumentOutOfRangeException`, leaking a domain-construction exception out of the malformed-file boundary instead of failing as template data corruption (`InvalidDataException`). The same store already routes invalid layer-mapping categories through explicit template-data validation.

## Validation plan

- Require both successful enum parse and `Enum.IsDefined` for persisted template family and rule categories before constructing domain objects.
- Add focused smoke coverage for malformed numeric family and rule category XML, expecting `InvalidDataException`.
- Preserve the existing valid template save/load/apply round trip.
- Re-fetch current `main` and both reserved blobs after claim visibility and use exact blob SHAs for writes.

## Coordination

Recent active claims reserve quantity settings, vertical placement, semantic views, room finishes, updater, family assignment, snapshot relations, documentation enumeration and other disjoint surfaces. No current recent claim reserves `TemplateProfileStore.cs` category parsing.

## Completion condition

Malformed numeric template categories fail closed at the XML/template boundary, valid templates still round-trip, the changes are reachable from current `main`, and this claim is closed with exact SHAs and truthful remote-only validation.