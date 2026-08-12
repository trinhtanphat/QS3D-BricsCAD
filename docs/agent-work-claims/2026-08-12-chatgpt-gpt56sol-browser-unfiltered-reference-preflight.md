# Work claim — browser unfiltered reference preflight

- Status: `COMPLETED`
- Agent: `chatgpt-gpt56sol-20260812-browser-unfiltered-preflight`
- Registered: `2026-08-12T09:42:00+07:00`
- Completed: `2026-08-12T09:45:00+07:00`
- Baseline main SHA: `4721cc060f242edc67e4d2ec14cb2981ce8e6f60`
- Claim commit: `dd5213b42b5d0edf6d15d2eacb334379be97803e`
- Implementation commit: `3990285a4fa98b5d1521f0c52eb5feaa43fe933e`
- Regression-test commit: `c26a1423f1c6ccf224f00e799e26deae3a6321b7`
- Final observed main during verification: `c26a1423f1c6ccf224f00e799e26deae3a6321b7`
- Priority: `Correctness regression discovered during requested continue-all review; ProjectBrowserQueryPlanner unfiltered mode bypassed Family/reference integrity validation that filtered mode still enforced.`

## Confirmed regression

The original Query Planner validated Family references before deciding whether a query was filtered. Commit `3644d795cd88552d1d4b53a20612159a51649183` optimized away an unnecessary unfiltered tree build but moved the full index/reference preflight after the unfiltered short-circuit. The same corrupt project could therefore pass in unfiltered mode and fail once a filter/search was enabled.

## Implemented

- Query/category normalization and filtered-state detection remain first.
- Existing element/family/floor/zone bounds, unique indexes and `ValidateElementReferences` now run before the unfiltered return.
- The optimization still builds exactly one browser tree on each execution path; no duplicate unfiltered tree construction was reintroduced.
- Canonical reference error wording is now mode-neutral (`Project browser query`) instead of incorrectly naming only the filtered path.

## Regression coverage

`ProjectBrowserQueryPlannerSmoke` now additionally proves:

- unfiltered mode rejects a missing Family reference;
- unfiltered mode rejects a Family/category mismatch;
- the existing healthy whitespace-only query still returns the complete unfiltered tree;
- existing filtered missing/mismatched/unmatched-reference and invalid-filter regressions remain intact.

## Changed surfaces

- `src/QS3D.Core/Navigation/ProjectBrowserQueryPlanner.cs`
- `tests/QS3D.Core.SmokeTests/ProjectBrowserQueryPlannerSmoke.cs`

## Validation performed

- Compared current source with the original feature commit and the later performance commit, identifying the exact short-circuit movement that introduced mode-dependent validation.
- Re-read current main after source/test publication and confirmed full reference preflight precedes the unfiltered return.
- Source and test writes used exact current blob SHAs with no force-push or concurrent-work overwrite.
- No GitHub Actions workflow was dispatched or rerun.
- No local .NET build or licensed BricsCAD V25/V26 runtime PASS is claimed from this remote source-only lane.

## Outcome

Project Browser query integrity is once again mode-independent: corrupt Family/reference state fails closed whether or not a filter/search is active, while the single-tree-build optimization is preserved. The lane is closed with no dangling ownership.
