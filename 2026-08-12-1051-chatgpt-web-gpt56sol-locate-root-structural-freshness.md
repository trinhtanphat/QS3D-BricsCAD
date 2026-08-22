# Work claim — Locate root structural freshness

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T10:51:00+07:00`
- Completed: `2026-08-12T10:53:00+07:00`
- Baseline main SHA: `9110226555dd310daa8188969ab543dfe74bb0a6`
- Claim commit: `f71ce67a874bb44733907a82c27df4245d82939d`
- Source commit on branch: `81cb68b667759bb453226e3d47b3c8002bc4244c`
- Regression-source commit on branch: `ee5fa37c68acf5b8aa562fbb18043ee845c4dca2`
- Pull request: `#784`
- Squash merge commit: `bad23bbf139f07548b40c8d02838927ca281a56d`
- Priority: evidence-driven Core Locate caller-input/project-ownership freshness

## Confirmed defect

`SourceHandleResolver.Resolve(ProjectState, IEnumerable<string>)` already pinned `ProjectState.ChangeVersion` while materializing caller-provided lazy root element IDs, but `project.Elements` is a publicly mutable list. A lazy root enumerable could directly remove or replace a selected `ProjectElement` instance without calling `ProjectState.Touch()`, leaving `ChangeVersion` unchanged and causing Locate to resolve the root ID against a different structural ownership state.

The completed sibling `SemanticHandleOwnershipResolver.Resolve(...)` structural-freshness contract provided the parity reference.

## Implemented

- Build and validate the current case-insensitive element-ID → exact `ProjectElement` instance index before caller root enumeration.
- Preserve the existing `ChangeVersion` freshness check across enumeration.
- Revalidate element count, IDs and exact object references after enumeration before missing-root checks or traversal.
- Reject direct removal/rebinding even when semantic `ChangeVersion` did not advance.
- Preserve root-ID normalization/bounds, missing-root diagnostics, dependency validation, Room Finish traversal, direct/boundary/generated handle fallback and deterministic output.

## Regression source

`SourceHandleResolverStructuralFreshnessSmoke` covers stable lazy root resolution, direct removal, same-ID replacement and mutating-empty input. Caller-side list mutations are intentionally not rolled back by this read-only resolver; the operation only fails closed before Locate consumes the changed ownership state.

## Integration evidence

While the branch was open, `main` advanced 7 commits, but `SourceHandleResolver.cs` retained exact pre-patch blob SHA `f1efad0b8dcff47e563187478e8ed0765c5d7b58`; no concurrent source overlap was present. PR `#784` was squash-merged with expected head SHA `ee5fa37c68acf5b8aa562fbb18043ee845c4dca2` into `bad23bbf139f07548b40c8d02838927ca281a56d`. Merged source was read back from `main` with blob SHA `90253191fe81d6fd8ce7aabc7ffc0b9f3a05b6a6`.

## Validation boundary

Remote/static source + regression review only. No GitHub Actions/build/release was dispatched and no executable .NET smoke/build or BricsCAD V25/V26 runtime PASS is claimed.
