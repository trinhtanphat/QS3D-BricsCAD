# Work claim — Project Browser filtered-query definition bounds

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-browser-query-definition-bounds-20260812-0735`
- Registered: `2026-08-12T07:35:00+07:00`
- Baseline main SHA: `63204ae7a5d6247ba574c71b7129152f5433e1b9`
- Priority: evidence-driven bounded enumeration during owner-requested `continue all`

## Confirmed defect

`ProjectBrowserQueryPlanner.Build(...)` bounds semantic elements before filtered-query processing, but then materializes complete Family/Floor/Zone dictionaries through `BuildUniqueFamilyIndex`, `BuildUniqueFloorIndex`, and `BuildUniqueZoneIndex` without first enforcing the existing domain capacities. A malformed or directly constructed `ProjectState` can therefore force the filtered-query boundary to enumerate/index more than the supported 10,000 Families or 2,000 Floor/Zone definitions before downstream `ProjectBrowserPlanner` bounds can run.

The existing domain services already fail closed at 10,000 Families (`ProjectFamilyService`) and 2,000 Floors/Zones (`ProjectFloorService` / `ProjectZoneService`). The previously completed browser Family/category-integrity lane is separate and intentionally preserved.

## Reserved scope

- `src/QS3D.Core/Navigation/ProjectBrowserQueryPlanner.cs` — filtered-query definition-count guards only
- `tests/QS3D.Core.SmokeTests/ProjectBrowserQueryDefinitionBoundsSmoke.cs` — focused Core regression
- `tests/QS3D.Core.SmokeTests/ProjectBrowserQueryDefinitionBoundsSmokeRegistration.cs` — isolated module-initializer registration
- this claim file for close-out

## Contract

- filtered queries reject more than 10,000 Family definitions before Family index enumeration;
- filtered queries reject more than 2,000 Floor definitions before Floor index enumeration;
- filtered queries reject more than 2,000 Zone definitions before Zone index enumeration;
- the exact supported boundaries remain accepted;
- existing query/filter/grouping, Family/category integrity, missing-reference, duplicate-ID and unfiltered `ProjectBrowserPlanner` behavior remain unchanged.

## Coordination / exclusions

- The completed `browser-family-category-integrity` claim is preserved; this lane does not alter its relation semantics.
- The completed base `ProjectBrowserPlanner` Floor/Zone definition-bound lane is preserved; this lane closes the earlier filtered-query indexing boundary only.
- No Workspace/UI/native BricsCAD, Family/Floor/Zone mutation, persistence, release/update, or unrelated navigation behavior changes.
- No GitHub Actions dispatch and no BricsCAD V25/V26 runtime PASS claim from this connector session.

## Validation plan

Use oversized collections containing an early null sentinel to prove cardinality guards run before definition enumeration/index validation, plus exact-boundary filtered-query coverage. Re-fetch current `main` and claim registry before substantive writes and preserve all concurrent work without force-push.
