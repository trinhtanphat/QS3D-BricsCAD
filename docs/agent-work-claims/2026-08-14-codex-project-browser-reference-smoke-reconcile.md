# Work claim — Project Browser reference smoke reconciliation

- Status: `COMPLETED`
- Agent: `codex-project-browser-reference-smoke-reconcile-20260814` (`/root/fix_source_reconcile_desync`)
- Registered: `2026-08-14T15:41:00+07:00`
- Baseline main SHA: `010aadc852a097d6704c4e4705e5b814c7185858`
- Priority: next deterministic Core full-smoke blocker after completed relation setter persistability

## Confirmed fixture drift

`ProjectBrowserReferenceCanonicalitySmoke` still expects the unfiltered planner to reject padded Floor/Zone setter assignments and whitespace-only Floor assignment. Supported `ProjectElement` relation setters now trim padded input before planner validation; whitespace-only Floor becomes the empty optional relation, which `ProjectBrowserPlanner` intentionally groups under `(No Floor)`.

Canonical and lower-case case-insensitive Floor/Zone relations remain reachable and valid. Missing Floor and duplicate semantic element identity remain covered by `ProjectBrowserPlannerSmoke`; planner duplicate-definition and exact lookup safeguards remain unchanged.

## Reserved scope

- `tests/QS3D.Core.SmokeTests/ProjectBrowserReferenceCanonicalitySmoke.cs`
- this claim only

Replace only the three stale throw expectations. Assert padded Floor/Zone writes store canonical values and still produce their canonical grouping nodes; assert whitespace-only Floor stores empty and produces the supported `(No Floor)` grouping node. Preserve and strengthen canonical plus lower-case case-insensitive grouping controls.

## Explicit exclusions

- no production planner, relation writer, persistence/schema, grouping or identity behavior changes;
- no focused gate change because no focused gate references this smoke or stale expectation;
- no LOCAL runner/probe/docs, issue `#1005`, BricsCAD/native/private data, GitHub Actions, release or packaging work;
- no edits to other Project Browser smokes; report the next full-smoke blocker rather than absorbing it.

## Validation

- Core Release build and full deterministic Core smoke;
- focused Project Browser family/category integrity and workspace schema gates;
- identity ambiguity, generic and manual-only policy gates;
- readback that missing-reference, unassigned and duplicate-identity Browser safeguards remain present and unchanged.

## Completion record

- Claim PR `#1227` merged as `4822a83320607e0d48812772b729796b9f7b2704`.
- Test commit `0676d00d3` merged through PR `#1229` as `05e00a7aecfa8e196f983c9d3b4fe914fdeadb98`.
- The one reserved smoke now verifies padded Floor/Zone setter writes store canonical values and produce their canonical grouping labels. Lower-case case-insensitive references resolve to the same labels, and whitespace-only Floor input stores empty and produces `(No Floor)`.
- Core Release build PASS with `0 warnings / 0 errors`. Project Browser family/category integrity, workspace schema and identity ambiguity gates PASS; generic and manual-only policy gates PASS. Missing-reference, unassigned and duplicate-identity planner fixtures remain present and unchanged.
- Full Core smoke advances beyond this Browser reference smoke and stops at the next independent fixture drift: `ProjectElementNullScalarPersistabilitySmoke.ConstructorTrimAndSetterExactnessRemain` still expects padded relation setter text to remain exact, while the completed relation setter now normalizes it. This lane did not edit or absorb that blocker.
- No production, focused gate, LOCAL, issue `#1005`, BricsCAD/native/private data or GitHub Actions surfaces were changed/run.
