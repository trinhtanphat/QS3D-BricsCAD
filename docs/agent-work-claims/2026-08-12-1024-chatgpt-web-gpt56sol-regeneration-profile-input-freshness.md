# Work claim — Regeneration profile subset input freshness

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T10:24:00+07:00`
- Completed: `2026-08-12T10:27:00+07:00`
- Baseline main SHA: `a41cac6f29c28440d21f3fc378ad31a5d5f3afc6`
- Claim commit: `5b9f6a503b0a65f00ec66f4404090ee5e9e815ab`
- Source commit on branch: `f1cbc5659a4cb26c6e4923647876e6077d3e93fc`
- Regression-source commit on branch: `a82a7aa58edeeb82dce07b7aa31926ac93bfe11c`
- Pull request: `#755`
- Squash merge commit: `344916aa923cca0be722bacc619896d410550161`
- Priority: evidence-driven Core caller-input/project-state freshness

## Confirmed defect

`RegenerationPreviewService.PreviewSubset(...)` establishes `SourceChangeVersion` before materializing caller-provided target IDs, and `RegenerationEngine.RegenerateDirtySubset(...)` likewise captures the project revision before target enumeration. `RegenerationWorkProfiler.ProfileSubset(...)` previously called `CanonicalTargetIds(...)` first and only captured `ChangeVersion` later inside `Build(...)`.

A side-effecting lazy target enumerable could therefore change the project during target materialization and the profiler would accept the post-enumeration revision as its baseline.

## Implemented

- `ProfileSubset(...)` now captures `ProjectState.ChangeVersion` and the project element-count bound before caller target enumeration.
- A changed revision is rejected immediately after target materialization, including the empty-target path.
- Existing canonicality/deduplication/count bounds, profiler null-entry integrity, graph validation, deterministic ordering and full-project `Profile(...)` behavior are unchanged.

## Regression source

`RegenerationWorkProfilerInputFreshnessSmoke` covers:

- stable lazy target input produces the expected subset profile and preserves the source revision;
- a lazy target enumerable that calls `project.Touch()` before yielding is rejected before profiling;
- a mutating lazy enumerable that yields no targets is still rejected before empty-profile behavior;
- caller-side revision changes are not falsely rolled back by this read-only profiler.

## Integration evidence

- The branch started after the immediately preceding profiler null-entry-integrity lane had completed.
- While the branch was open, `main` advanced 10 commits, but `RegenerationWorkProfiler.cs` retained exact pre-patch blob SHA `1c6387f661fe4371a3ca0e425f3e79ea718be6cd`; no concurrent source overlap was present.
- PR `#755` was squash-merged with expected head SHA `a82a7aa58edeeb82dce07b7aa31926ac93bfe11c` into `344916aa923cca0be722bacc619896d410550161`.
- Source and regression were read back directly from `main` after merge.

## Validation boundary

Remote/static source + regression review only. No GitHub Actions/build/release was dispatched, smoke source was not executed in this web session, and no BricsCAD V25/V26 or local .NET runtime PASS is claimed.
