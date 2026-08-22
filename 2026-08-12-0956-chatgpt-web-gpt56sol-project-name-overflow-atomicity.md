# Work claim — ProjectState Name ChangeVersion-overflow atomicity

- Status: `COMPLETED`
- Outcome: `REDUNDANT_CONCURRENT_FIX` — defect confirmed, but concurrent work integrated the final fix before this branch could merge.
- Agent: `chatgpt-web-gpt56sol-project-name-overflow-atomicity-20260812-0956`
- Registered: `2026-08-12T09:56:00+07:00`
- Baseline main SHA: `2808d90412f298dee0e008a7806a7e898c360366`

## Confirmed defect

PR #722 initially assigned `_name = next` before the checked freshness increment. At `ChangeVersion == long.MaxValue`, rename could throw after changing Name while leaving ChangeVersion/UpdatedUtc unchanged.

## Concurrent completion observed on main

Parallel completed claim `2026-08-12-0955-gpt56sol-project-name-overflow-atomicity.md` integrated the same defect first:

- source fix `0255a53315e1b624fc88b6b6a0f48082c51bfc81`
- regression `46105e82c54012468625a1cf5155e14cdc758678`
- current setter precomputes checked next ChangeVersion and timestamp before mutating Name/freshness.

## Redundant branch evidence

- claim commit `6d3bdd42b153198bda216e7692a555a06df5800f`
- branch source `966556436c3db682aa7ee3eb4db2f1df62c84605`
- branch smoke `45814654d704622e57c77c9b38dc664397b53e6d`
- PR #730 intentionally closed unmerged after comparison showed concurrent `ProjectState.cs` overlap.

No source/test from this duplicate branch was merged into main.

## Validation boundary

Current-main source/claim readback plus exact overlap comparison. No GitHub Actions were dispatched and no licensed BricsCAD V25/V26 runtime PASS is claimed.
