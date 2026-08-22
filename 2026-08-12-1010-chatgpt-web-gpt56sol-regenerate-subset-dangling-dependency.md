# Work claim — Subset regeneration dangling dependency integrity

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T10:10:00+07:00`
- Completed: `2026-08-12T10:13:00+07:00`
- Baseline main SHA: `90e9b4a863cafbde898993a3395438ad24f4cd23`
- Claim commit: `d6e5b50966c27d62b7a7754f378c6316fe2cdab5`
- Source commit on branch: `6fc4dc69d556009178ebdeb6c995531d0e65a813`
- Regression-source commits on branch: `21cfd4a4a728f019fc509f8d32b177e7d8540843`, `3fb2c655b88ced2a6b723913177409ecddf42951`
- Pull request: `#740`
- Squash merge commit: `f571c3b49c7280858ca6a1a409841ff0d73898aa`
- Priority: evidence-driven Core targeted-regeneration relation integrity

## Confirmed defect

`RegenerationEngine.RegenerateDirtySubset(...)` scans the complete `project.Elements` collection to resolve requested targets, so it can distinguish an in-project dependency outside the selected subset from a dependency whose semantic target is missing entirely. Previously it did not perform that distinction. `DependencyGraph.TopologicalDirtyOrder(...)` intentionally ignores dependencies outside the supplied candidate set, so a selected dirty element could depend on `MISSING` and still regenerate.

## Implemented

- After requested targets are resolved and unknown-target precedence is preserved, selected targets with canonical unique dependency lists are checked against the complete project identity set.
- A dependency that exists in `project.Elements` remains valid even when it is clean and outside the selected subset.
- A dependency whose target is absent from the project now fails closed with the existing full-graph missing-dependency message before regeneration mutation.
- Blank, padded/non-canonical and duplicate dependency lists are deliberately left for existing `TopologicalDirtyOrder(...)` validation so those established diagnostics retain precedence.
- `DependencyGraph` and full-project `RegenerateDirty()` behavior are unchanged.

## Regression source

`RegenerationSubsetDependencyIntegritySmoke` covers:

- dangling selected dependency rejection before project/dirty/quantity mutation;
- valid dirty target depending on a clean in-project element outside the selected subset;
- unknown requested target remains higher-precedence than selected dependency-integrity validation.

The unknown-target fixture intentionally includes an unrelated second project element so the existing target-count bound does not preempt the intended `KeyNotFoundException` branch.

## Integration evidence

- While the branch was open, `main` advanced 7 commits, but `RegenerationEngine.cs` retained exact pre-patch blob SHA `cf49b46fd42d3d521227e15e62534194b6aa7a73`; no concurrent source overlap was present.
- PR `#740` was squash-merged with expected head SHA `3fb2c655b88ced2a6b723913177409ecddf42951` into `f571c3b49c7280858ca6a1a409841ff0d73898aa`.
- Source and regression were read back directly from `main` after merge.

## Validation boundary

Remote/static source + regression review only. No GitHub Actions/build/release was dispatched, smoke source was not executed in this web session, and no BricsCAD V25/V26 or local .NET runtime PASS is claimed.
