# Work claim — Semantic untrack predicate freshness

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T10:19:00+07:00`
- Completed: `2026-08-12T10:21:00+07:00`
- Baseline main SHA: `ee2befc6ea239b20528a6837dd8d1e2ba19161e1`
- Claim commit: `d92e143d74d01adeb5f466b4943dd1e3a233f8c5`
- Source commit on branch: `08f5c18ca25e198f47eb96e0078bdf02314e8a72`
- Regression-source commit on branch: `67b4f55bf5adfbe55b3a51211b4c4457408351ff`
- Pull request: `#749`
- Squash merge commit: `fc83b2e2c81694ab3f1f6bb56ebecc7ca523d727`
- Priority: evidence-driven Core caller-callback/project-state freshness

## Confirmed defect

`SemanticUntrackService.Untrack(...)` first resolves semantic ownership, then evaluates the caller-provided optional `predicate` over resolved project-owned elements. The predicate is arbitrary caller code, but the service previously did not pin `ProjectState.ChangeVersion` while that callback executed. A predicate could therefore change the project revision and return a target, after which dependency blockers and untrack mutation continued against a different project revision than the one used for ownership resolution.

## Implemented

- Ownership resolution remains unchanged and occurs before predicate filtering.
- A null predicate preserves the existing direct target list behavior.
- For a caller predicate, `ProjectState.ChangeVersion` is captured immediately before callback evaluation and checked immediately after materialization.
- A changed project revision fails closed before dependency blocker planning, semantic removal, or a filtered empty-result no-op.
- Caller-side changes already performed by the predicate are not falsely rolled back by this boundary.

## Regression source

`SemanticUntrackPredicateFreshnessSmoke` covers:

- stable predicate removes the owned semantic element and advances the project revision once;
- mutating predicate returning `true` is rejected before removal while retaining its caller-side `Touch()`;
- mutating predicate returning `false` is also rejected before the filtered no-op path.

## Integration evidence

- While the branch was open, `main` advanced 4 commits, but `SemanticUntrackService.cs` retained exact pre-patch blob SHA `cb19bcc3c30c5c62230a0976f55a70da31379bab`; no concurrent source overlap was present.
- PR `#749` was squash-merged with expected head SHA `67b4f55bf5adfbe55b3a51211b4c4457408351ff` into `fc83b2e2c81694ab3f1f6bb56ebecc7ca523d727`.
- Source and regression were read back directly from `main` after merge.

## Validation boundary

Remote/static source + regression review only. No GitHub Actions/build/release was dispatched, smoke source was not executed in this web session, and no BricsCAD V25/V26 or local .NET runtime PASS is claimed.
