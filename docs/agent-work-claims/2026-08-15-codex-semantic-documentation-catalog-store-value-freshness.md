# Work claim — Semantic Documentation Catalog Store planner-value freshness

- Status: `ACTIVE`
- Agent: `Codex /root/audit_docs_next`
- Registered: `2026-08-15T10:52:11+07:00`
- Baseline main SHA: `c5fbe4af9fb98383679f279e33d9b93eb2ec737d`
- Issue: `#77`
- Priority: remote-safe Core documentation correctness

## Confirmed defect

`SemanticDocumentationCatalogStore.Save(...)` snapshots `ProjectState.ChangeVersion` plus the ordered object-reference identity of Elements, Floors, and Zones before it enumerates caller-controlled `views` and `sheets`. Its five freshness checks do not snapshot planner-relevant values inside an existing `ProjectElement`.

A lazy view or sheet enumerable can mutate the same project-owned element instance's `Category`, `FloorId`, or `ZoneId` without calling `ProjectState.Touch()` or replacing the list entry. The Store freshness checks still pass because the revision and references are unchanged. `SemanticViewPlanner.BuildCatalog(...)` then captures its own snapshot only after both Store enumerations, plans the changed state, and the Store publishes documentation metadata despite project drift during the save operation.

Concrete counterexample: `E-01` starts as a Beam. Lazy view enumeration changes `project.Elements[0].Category` to Column and yields a Column-filtered view. Current Store source persists the resulting catalog instead of rejecting the mid-save planner-value change.

## Reserved scope

- `src/QS3D.Core/Documentation/SemanticDocumentationCatalogStore.cs`: extend the existing Store project snapshot/checks to pin each ordered element's `Id`, `Category`, `FloorId`, and `ZoneId` values at all five established freshness boundaries.
- `tests/QS3D.Core.SmokeTests/SemanticDocumentationCatalogSaveStructuralFreshnessSmoke.cs`: extend the existing auto-registered smoke with lazy-view and lazy-sheet same-instance planner-value drift rejection while preserving stable deterministic-save coverage.
- `scripts/preflight-semantic-documentation-catalog-save-structural-freshness.py`: extend the existing focused static guard only.
- This claim file for registration and handoff.

## Intended contract

- Capture planner-relevant element values before enumerating either caller-controlled catalog input.
- Reject same-instance `Category`, `FloorId`, or `ZoneId` drift at every existing Store freshness boundary, including immediately before either persistence mutation.
- Preserve the existing five-check ordering, revision/count/order/reference-identity checks, view/sheet bounds, planner validation, deterministic XML, no-op behavior, and mutation atomicity.

## Explicit exclusions

- No changes to `SemanticViewPlanner`, Semantic Schedule, documentation-table rendering, catalog XML schema/text rules, or catalog editor semantics.
- No BricsCAD V25/V26, native, UI, runtime, LOCAL runner/probe, private-data, release/package/signing, workflow, or GitHub Actions work.
- Broad issue `#77` remains open.

## Validation plan

- Run the extended focused smoke/static gate plus the existing Semantic View/Sheet/Schedule/TitleBlock documentation gates.
- Build `QS3D.Core` and `QS3D.Core.SmokeTests` in Release and run the full deterministic Core smoke suite.
- Run aggregate remote-safe preflight and report any independent blocker without expanding scope.
- Re-fetch `origin/main` before push and final handoff, preserve concurrent work, and stop before merge.
