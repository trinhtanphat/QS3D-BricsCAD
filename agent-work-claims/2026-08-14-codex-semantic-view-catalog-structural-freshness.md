# Work claim — Semantic View catalog structural freshness

- Status: `COMPLETED`
- Agent: `/root/fix_source_reconcile_desync`
- Registered: `2026-08-14T16:02:57+07:00`
- Baseline main SHA: `968761f9cf97850cb3e43f3b5e009e04b7765f07`
- Issue: `#77`
- Priority: remote-safe public Core documentation correctness

## Verified gap

`SemanticViewPlanner.BuildCatalog(ProjectState, IEnumerable<SemanticViewDefinition>)` materializes the caller-controlled definition sequence before it first validates or snapshots the project. A lazy sequence can replace an entry in the public mutable `ProjectState.Elements` list with a new same-ID instance without advancing `ChangeVersion`, then yield a definition whose filters select the replacement. The planner currently returns a catalog derived from state that changed during input enumeration instead of rejecting the cross-enumeration structural drift.

## Reserved scope

- `src/QS3D.Core/Documentation/SemanticViewPlanner.cs`: `BuildCatalog` project revision and ordered reference-identity freshness across definition enumeration only.
- `tests/QS3D.Core.SmokeTests/SemanticViewCatalogStructuralFreshnessSmoke.cs` and its registration: prove same-ID element replacement and revision-only drift fail closed, with a stable deterministic control.
- `scripts/preflight-semantic-view-catalog-structural-freshness.py`: bind snapshot-before-materialization and check-after-enumeration/before-return ordering.
- this claim document for closeout only.

## Intended contract

- Capture `ProjectState.ChangeVersion` and the ordered reference identities of `Elements`, `Floors`, and `Zones` before enumerating `definitions`.
- Reject revision, count, order, or instance replacement drift immediately after definition enumeration and again before returning the catalog.
- Preserve the existing 10,000-definition bound, null/identity/reference/category/filter validation, duplicate ID/name rejection, deterministic ordering, and defensive read-only result.

## Explicit exclusions

- No changes to `SemanticViewPlanner.Build`, other documentation planners/catalog persistence, the active `SemanticDocumentationTableBuilder` structural-freshness lane, or native BricsCAD documentation workflows.
- No LOCAL runner/probe/docs, BricsCAD/private data, GitHub Actions, release, packaging, UI, or runtime work.

## Validation

- focused new smoke and static gate plus existing semantic View/Sheet/Schedule/TitleBlock gates;
- `QS3D.Core` and `QS3D.Core.SmokeTests` Release builds;
- full deterministic Core smoke, reporting the first unrelated blocker without expanding scope.

## Completion

- Claim PR `#1252` merged as `a8ad1db5f988b885c3a2c79531dca539ab1c28f6` before implementation began.
- Source PR `#1260` merged as `77b28623cf7f0d45a01da601b3875d93f5e4b4fe`.
- Focused structural-freshness gate: `PASS` on the exact merged source.
- Twenty Core View/Sheet/Schedule/TitleBlock/documentation preflights: `PASS`.
- `QS3D.Core` and `QS3D.Core.SmokeTests` Release builds: `0` warnings, `0` errors.
- Full deterministic Core smoke reached the unrelated `ProjectStatePersistedScalarVersioningSmoke.PersistedScalarsAdvanceVersionExactlyOnce` fixture, whose padded `ActiveZoneId` expectation predates current canonical relation storage. This lane did not expand into that fixture.
- Final implementation remained limited to `SemanticViewPlanner.BuildCatalog`, one auto-registered focused smoke, and one focused static gate; no native/runtime/LOCAL/Actions surface changed.
