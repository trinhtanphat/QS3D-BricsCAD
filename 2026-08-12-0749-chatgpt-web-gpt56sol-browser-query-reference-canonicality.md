# Work claim — filtered Project Browser reference canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-browser-query-reference-canonicality-20260812-0749`
- Registered: `2026-08-12T07:49:00+07:00`
- Baseline main SHA: `117f529eaf88b8b30ddc8a788e849924915f0eb6`
- Priority: P2 — keep filtered Browser queries from silently normalizing semantic relation state rejected by QSDB.

## Confirmed defect

`ProjectBrowserQueryPlanner` uses a separate validation path whenever search/filtering is active. That path trimmed mutable `ProjectElement.FamilyId`, `FloorId`, and `ZoneId` before lookup, allowing padded semantic relations to pass filtered-query validation even though QSDB persistence rejects the same non-canonical state.

## Implemented fix

- Filtered-query validation now fails closed when non-empty element `FamilyId`, `FloorId`, or `ZoneId` is whitespace-only or contains leading/trailing whitespace.
- Empty optional relations and case-insensitive canonical IDs remain supported.
- Query/filter matching, user-supplied filter-ID whitespace behavior, family/category integrity checks, query-option/definition bounds, and ordinary unfiltered delegation remain unchanged.
- Focused smoke coverage pins canonical lower-case references plus padded Family/Floor/Zone and whitespace-only Family rejection.

## Reserved surfaces

- `src/QS3D.Core/Navigation/ProjectBrowserQueryPlanner.cs`
- `tests/QS3D.Core.SmokeTests/ProjectBrowserQueryReferenceCanonicalitySmoke.cs`
- this claim file

## Integration evidence

- Claim registration: `d95f0348de9ba441257e443e8ecdfabe637caab0`.
- Branch source commit: `6c0d69fb4097c52f0c466e84524bde78c2903156`.
- Branch smoke commit: `18e6d9a0a655f777928dc2a307889988eb81fd4e`.
- Branch diff was exactly the reserved query-planner source plus new focused smoke (+12/-3 source lines).
- Comparison from claim registration to then-current `main` `a6782d5321bf8a431099aaafeeb1a9f362984d1c` showed 19 intervening commits and no modification of either reserved path.
- PR `#627` squash-merged cleanly at `3d51e127c95502b289a226829b5c852d7f2f532d`.

## Validation boundary

Committed deterministic Core smoke coverage plus exact source/diff review. No GitHub Actions were dispatched and no BricsCAD V25 runtime PASS is claimed.
