# Work claim — Project Browser filtered-query definition bounds

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-browser-query-definition-bounds-20260812-0735`
- Registered: `2026-08-12T07:35:00+07:00`
- Completed: `2026-08-12T07:36:00+07:00`
- Baseline main SHA: `63204ae7a5d6247ba574c71b7129152f5433e1b9`
- Reservation commit: `b308710488d14669473bfa3f20d533d0184b6ee0`
- Priority: evidence-driven bounded enumeration during owner-requested `continue all`

## Defect fixed

`ProjectBrowserQueryPlanner.Build(...)` bounded semantic elements before filtered-query processing but materialized complete Family/Floor/Zone dictionaries without first enforcing the existing domain capacities. A malformed or directly constructed `ProjectState` could therefore force the filtered-query boundary to enumerate/index more than the supported 10,000 Families or 2,000 Floor/Zone definitions before downstream planner bounds ran.

The filtered-query boundary now rejects oversized Family/Floor/Zone definition collections before any `BuildUnique*Index` enumeration. The limits match the existing domain services: 10,000 Families and 2,000 Floors/Zones.

## Published commits

- `e863db1bf1a3693c42440b0e34f2955c18ca0fbc` — `fix(browser): bound filtered query definitions`
- `f9b740f27f5724ed5b8986767ee684911dd3fb5c` — `test(browser): guard filtered query definition bounds`
- `ecd6e3d8cfb898e95b13755441cf4c195fcd6b7c` — `test(browser): register query definition bounds smoke`

## Preserved contract

- filtered queries reject more than 10,000 Family definitions before Family index enumeration;
- filtered queries reject more than 2,000 Floor definitions before Floor index enumeration;
- filtered queries reject more than 2,000 Zone definitions before Zone index enumeration;
- exactly 10,000 Families plus 2,000 Floors and 2,000 Zones remains supported by the filtered-query boundary;
- existing query/filter/grouping, Family/category integrity, missing-reference, duplicate-ID and unfiltered `ProjectBrowserPlanner` behavior remain unchanged.

## Regression evidence

`ProjectBrowserQueryDefinitionBoundsSmoke` uses an early null sentinel in each oversized definition collection and asserts the cardinality-specific exception. This pins fail-fast ordering: if an index were enumerated first, the test would receive the existing null-definition validation error instead. The exact-boundary case constructs all supported definition counts, executes a real filtered query, and verifies an empty filtered root is returned normally. Registration uses the repository's existing `[ModuleInitializer]` convention and does not edit the shared runner.

## Coordination / validation boundaries

- The completed `browser-family-category-integrity` relation semantics were preserved.
- The completed base `ProjectBrowserPlanner` Floor/Zone definition-bound behavior was preserved.
- No Workspace/UI/native BricsCAD, Family/Floor/Zone mutation, persistence, release/update, or unrelated navigation behavior changed.
- Source and smoke changes were written through current-blob GitHub Contents API operations without force-push.
- Connector validation inspected source/test/registration and repository ancestry only; no local `dotnet`/Core smoke execution is claimed.
- No GitHub Actions were dispatched and no BricsCAD V25/V26 runtime PASS is claimed.
