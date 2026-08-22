# Work claim — Dependency Impact structural freshness

- Status: `ABANDONED`
- State: `ABANDONED`
- Agent: `chatgpt-web-gpt56sol-dependency-impact-structural-freshness-20260812-1149`
- Registered: `2026-08-12T11:49:00+07:00`
- Abandoned: `2026-08-12T11:53:00+07:00`
- Baseline main SHA: `86ed1ecf7ce2189f9ba64b35354dea6f0fb695b4`
- Claim commit: `6eac72ba321660e2b632088b881d47e118fed208`
- Superseded PR: `#843` — closed without merge
- Priority: P1 — read-only impact planning must not silently switch to a different semantic element structure during caller-controlled enumeration.
- Task Key: `CORE-DEPENDENCY-IMPACT-STRUCTURAL-FRESHNESS`

## Confirmed defect

The completed Dependency Impact input-freshness lane moved the `ChangeVersion` snapshot before lazy root enumeration, so ordinary semantic mutations that call `ProjectState.Touch()` are detected. However `ProjectState.Elements` remains a public mutable list. A caller-provided lazy `sourceElementIds` sequence can directly add/remove/reorder/replace entries while it is enumerated without advancing `ChangeVersion`. The planner could then rebuild `DependencyGraph` from changed structural ownership while retaining the pre-enumeration revision.

## Abandonment / coordination reason

This reservation was discovered to be a later duplicate of the already-registered owner claim `docs/agent-work-claims/2026-08-12-1148-chatgpt-web-gpt56sol-dependency-impact-source-structural-freshness.md`, whose claim commit `6de6e0897aaefe068b0a968ef086ac1386eed085` predates this claim. That owner independently integrated the same defect class on current `main` as source commit `14b593976950ac5d40ed95ca6c4f4adcc56ea747`, using an ID-to-instance ownership snapshot and structural freshness checks before graph planning and before return.

The branch created from this later claim (`agent/dependency-impact-structural-freshness-20260812`) is therefore not eligible for integration. PR #843 was closed without merge because merging it would overwrite/revert the prior owner's implementation.

## Work not integrated from this claim

- branch source commit `e6366a0ae2abccf1ecd85494356c2d1023bfd00f`
- branch smoke commit `181dfae943b704b898e476379e0b4c8dba1ececf`

Neither branch commit was merged to `main` by this claim.

## Validation boundary

No GitHub Actions were dispatched. No force-push was used. No local .NET/full executable smoke or licensed BricsCAD V25/V26 runtime PASS is claimed.
