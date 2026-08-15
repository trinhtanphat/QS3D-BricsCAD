# Work claim — Semantic View catalog planner-value freshness

- Status: `COMPLETED`
- Agent: `Codex /root/audit_docs_next`
- Registered: `2026-08-15T10:34:00+07:00`
- Completed: `2026-08-15T10:42:00+07:00`
- Baseline main SHA: `7e9dfadc9151b3fd2585d15ad25ed0d5146b7deb`
- Issue: `#77`
- Priority: remote-safe Core documentation correctness

## Confirmed defect

`SemanticViewPlanner.BuildCatalog(ProjectState, IEnumerable<SemanticViewDefinition>)` snapshots `ProjectState.ChangeVersion` plus the ordered object-reference identity of Elements, Floors, and Zones before it enumerates caller-controlled definitions. The freshness checks do not snapshot planner-relevant values inside an existing `ProjectElement`.

A lazy definition enumerable can mutate the same project-owned element instance's `Category`, `FloorId`, or `ZoneId` without calling `ProjectState.Touch()` or replacing the list entry, then yield a definition whose filters select the changed values. Both existing freshness checks pass and the planner returns a catalog derived from project values changed during external enumeration.

Concrete counterexample: `E-01` starts as a Beam. Definition enumeration changes `project.Elements[0].Category` to Column and yields a Column-filtered view. Current source returns `E-01` in that view instead of rejecting caller-enumeration project drift.

## Reserved scope

- `src/QS3D.Core/Documentation/SemanticViewPlanner.cs`: extend the existing `BuildCatalog` project snapshot/checks to pin the ordered element reference plus the planner-relevant `Id`, `Category`, `FloorId`, and `ZoneId` values across external definition enumeration.
- `tests/QS3D.Core.SmokeTests/SemanticViewCatalogStructuralFreshnessSmoke.cs`: extend the existing auto-registered smoke with same-instance category/relation drift rejection and preserve its stable deterministic/read-only control.
- `scripts/preflight-semantic-view-catalog-structural-freshness.py`: extend the existing focused static guard to require planner-value freshness while preserving the two established check boundaries.
- This claim file for registration and handoff.

## Intended contract

- Capture planner-relevant element values before enumerating `definitions`.
- Reject same-instance Category, FloorId, or ZoneId drift immediately after definition enumeration and again before returning the catalog.
- Preserve existing revision, count, order, and reference-identity freshness checks.
- Preserve the 10,000-definition bound, null/identity/reference/category/filter validation, duplicate ID/name rejection, deterministic ordering, and defensive read-only result.

## Explicit exclusions

- No changes to single-definition `SemanticViewPlanner.Build(...)` behavior.
- No changes to `SemanticDocumentationCatalogStore`, the actively reserved `SemanticDocumentationTableBuilder`, Semantic Schedule/Sheet/Tag behavior, XML format/schema, or documentation editor behavior.
- No BricsCAD/native/UI/runtime/private-data work, LOCAL runner/probe changes, release/package/signing changes, workflows, or GitHub Actions operations.
- Broad issue `#77` remains open.

## Validation plan

- Run the extended focused smoke and static gate plus the existing Semantic View/Sheet/Schedule documentation gates.
- Build `QS3D.Core` and `QS3D.Core.SmokeTests` in Release and run the full deterministic Core smoke suite.
- Run aggregate remote-safe preflight and report any independent blocker without expanding scope.
- Re-fetch `origin/main` before push and final handoff, preserve concurrent work, and stop before merge.

## Completion evidence

- Claim commit `6acc01b537f24f5b90e91f32d3bfc5cc56ecf171` merged first through PR `#1542` at `d73420a2dce589fd74e220efdcca3071b828b335` before implementation began.
- Implementation commit `f54919e95e3915f0799ba70e4c5b115f10dcc74b` merged through PR `#1552` at exact main SHA `ca8b2b38557c169c12526d89c0513c601f70a1db`.
- `SemanticViewPlanner.BuildCatalog(...)` now snapshots and rechecks each ordered element's `Id`, `Category`, `FloorId`, and `ZoneId` values alongside the existing revision and reference-identity snapshot at both established freshness boundaries.
- The existing auto-registered structural-freshness smoke now proves same-instance category drift and combined Floor/Zone relation drift fail closed while the stable deterministic/read-only control remains intact.
- On the refreshed implementation candidate, all `27` focused Documentation/View/Sheet/Schedule/TitleBlock static gates passed; `QS3D.Core` and `QS3D.Core.SmokeTests` Release builds completed with `0` warnings and `0` errors; full deterministic Core smoke reported `ALL PASS`; `scripts/preflight.py` passed.
- Aggregate feature preflight passed `812/814` gates. The only failures were unrelated existing release-sync and Wall Quantity Locate ordering gates; this lane did not modify either surface.
- Root revalidated exact merged main `ca8b2b38557c169c12526d89c0513c601f70a1db`: the focused structural-freshness gate passed, both Release builds completed with `0` warnings and `0` errors, and full Core smoke reported `ALL PASS`.
- No BricsCAD/native/UI/runtime/private-data operation or GitHub Actions dispatch/rerun was performed. Broad issue `#77` remains open for its larger native documentation scope.
