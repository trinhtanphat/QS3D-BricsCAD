# Work claim — Source Handle root freshness

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-source-handle-root-freshness-20260812-0850`
- Registered: `2026-08-12T08:50:00+07:00`
- Completed: `2026-08-12T08:55:00+07:00`
- Baseline main SHA: `953bc91e46bfbcbb2e089080e1d647f6529c74ac`
- Claim commit: `ddd28e0b363dc6acd5a6af76c4f7c7829fda88d6`
- Source fix commit: `01e339bd877ce922e76d8dcf60e5dc61457d0bce`
- Regression commit: `9ea8a9df7aef569d63840e0d28d5b5cf7403a025`

## Completed scope

`SourceHandleResolver.Resolve` now captures `ProjectState.ChangeVersion` immediately before materializing caller-controlled root IDs, fails closed if that enumeration changes the project, and only then builds the semantic element index. This prevents Locate from querying a pre-enumeration stale index and silently omitting a root that became valid while the lazy enumerable ran.

## Implemented surfaces

- `src/QS3D.Core/Services/SourceHandleResolver.cs`
- `tests/QS3D.Core.SmokeTests/SourceHandleRootFreshnessSmoke.cs`
- this claim file

## Validation actually performed

- Re-read integrated `SourceHandleResolver` from current `main` and confirmed ordering is root materialization → project freshness comparison → element-index build.
- Re-read `SourceHandleRootFreshnessSmoke` from current `main`. It covers a lazy sequence that adds/touches/yields a late root, a lazy sequence that touches while yielding an existing root, and a side-effect-free direct-handle Locate path.
- Verified regression commit `9ea8a9df7aef569d63840e0d28d5b5cf7403a025` remains an ancestor of current main snapshot `816e9cc7a0141749c818e315713a1fdbc8d33e15` with `behind_by: 0`; the one intervening commit only closed a separate Curtain Opening claim.
- No local .NET build/smoke execution is claimed from this connector-only environment.
- No GitHub Actions were dispatched and no BricsCAD V25/V26 runtime PASS is claimed.

## Preserved behavior

Existing root input bounds, dependency validation, direct/boundary/generated handle precedence, source ownership semantics and normal read-only Locate behavior were not redesigned.

## Completion

Completed. Current `main` guards project freshness across Source Handle root enumeration, builds its element index only after stable materialization, focused regression source is committed, and exact SHAs are recorded above.