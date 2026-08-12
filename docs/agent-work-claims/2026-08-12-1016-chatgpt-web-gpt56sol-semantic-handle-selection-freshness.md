# Work claim — Semantic handle selection input freshness

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T10:16:00+07:00`
- Completed: `2026-08-12T10:19:00+07:00`
- Baseline main SHA: `67a7ca73b0fff9c626bfeba7cebdc4c00a50455f`
- Claim commit: `0549357ae4a7d3446118107fc68394e1dd394787`
- Source commit on branch: `2228ea40d76c8989131f77fc9de5cf3022d4289f`
- Regression-source commit on branch: `fad0d5a40a569fa7c27fbcc6d407da940f1df4e0`
- Pull request: `#745`
- Squash merge commit: `17003f46fe1930a45f6a777f64497069f8e51321`
- Priority: evidence-driven Core caller-input/project-state freshness

## Confirmed defect

`SourceHandleResolver.Resolve(...)` pins `ProjectState.ChangeVersion` while materializing caller-provided lazy root element IDs, then reads project ownership from the same semantic revision. `SemanticHandleOwnershipResolver.Resolve(...)` previously performed `EnsureUniqueElementIds(project)` first, then materialized caller-provided lazy `selectedHandles` without checking whether that enumeration changed the project before ownership scanning continued.

A side-effecting lazy handle enumerable could therefore establish ownership resolution against a different project revision than the one whose element identity integrity was initially validated.

## Implemented

- Existing malformed-project element-ID validation remains first.
- `ProjectState.ChangeVersion` is captured immediately before caller-provided selected handles are materialized.
- A changed project revision now fails closed before ownership scanning or empty-selection no-op.
- Existing selected-handle normalization/deduplication/count bound, ownership conflict diagnostics, canonical stored-handle validation and deterministic ordering are unchanged.

## Regression source

`SemanticHandleSelectionFreshnessSmoke` covers:

- stable lazy handle input resolves the expected owned element without mutating the project;
- lazy input that calls `project.Touch()` then yields a handle is rejected before ownership scanning;
- lazy input that calls `project.Touch()` then yields no handles is still rejected before empty-selection no-op;
- caller-side project mutation is not falsely rolled back by this read-only resolver.

## Integration evidence

- While the branch was open, `main` advanced 10 commits, but `SemanticHandleOwnershipResolver.cs` retained exact pre-patch blob SHA `3b50d8106395d9329a0eb9ac5d9e820c04f2fcdb`; no concurrent source overlap was present.
- PR `#745` was squash-merged with expected head SHA `fad0d5a40a569fa7c27fbcc6d407da940f1df4e0` into `17003f46fe1930a45f6a777f64497069f8e51321`.
- Source and regression were read back directly from `main` after merge.

## Validation boundary

Remote/static source + regression review only. No GitHub Actions/build/release was dispatched, smoke source was not executed in this web session, and no BricsCAD V25/V26 or local .NET runtime PASS is claimed.
