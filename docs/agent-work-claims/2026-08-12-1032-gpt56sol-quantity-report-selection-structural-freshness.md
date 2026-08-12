# Work claim — quantity report selection structural freshness

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-quantity-report-selection-structural-freshness-20260812-1032`
- Registered: `2026-08-12T10:32:00+07:00`
- Completed: `2026-08-12T10:55:00+07:00`
- Baseline main SHA recorded at registration: `49a4e0345e2c82e259d28bed1b53580a25e527fc`
- Actual claim parent SHA: `8614ffb17b5c851f013a127319c53b6ef9a516b9`
- Claim commit: `2a725b3b5da4473b33fbe13577933c9dde3f005c`
- Source integration commit: `a207c0c87f23800e39cbd8aefd818bfb9f2ef2df`
- Regression integration commit: `79cc183f255d9b0be4ba2fb90a470c243a298828`
- Superseded PRs: `#769`, `#778` — both closed without merge because their frozen moving-base snapshots accumulated unrelated concurrent files
- Priority: evidence-driven remote-safe reporting consistency

## Completed scope

`ProjectQuantityReportBuilder.Group/Detail(project, elementIds)` now captures the exact `ProjectElement` instance resolved for each semantic id while a lazy selection enumerable is consumed. After enumeration completes, project element identity is revalidated and each selected id must still resolve to the exact same object instance. Removal or rebinding under the same id therefore fails closed instead of silently returning a partial report or reporting a replacement object.

Existing blank/duplicate/missing selection behavior, case-insensitive semantic-id matching, report calculations, grouping/detail semantics and output ordering remain unchanged.

## Implemented surfaces

- `src/QS3D.Core/Reporting/ProjectQuantityReportBuilder.cs`
- `tests/QS3D.Core.SmokeTests/ProjectQuantityReportSelectionStructuralFreshnessSmoke.cs`
- this claim file

## Integration / concurrency evidence

- Feature source commit: `f7d33d9f35addd5ab17e6979501d96eef16a5ac7`.
- Feature regression commit: `6f4db812b98571850cb29eeba86b2ee63b73f26c`.
- Exact reviewed production blob: `bdbed57801c7bdc760431295ec3ce4464c9641fc`.
- Exact reviewed smoke blob: `10df8551419a7eeebf6822e9ddaae8bae276f354`.
- Multiple moving-main comparisons found no concurrent edit to `ProjectQuantityReportBuilder.cs` or the focused smoke while this lane was active.
- Branches were repeatedly refreshed with current-main trees plus the exact reviewed blobs using two-parent commits and `force: false`; no force-push was used.
- PR `#769` and replacement PR `#778` were deliberately closed without merge because GitHub retained old base snapshots and their displayed diffs accumulated unrelated concurrent claim/preflight files despite the feature head carrying only the reviewed source/smoke delta.
- Two whole-ref fast-forward attempts to `main` failed safely with `Update is not a fast forward` when `main` advanced between commit creation and ref update; neither failed attempt changed shared history.
- Final integration used GitHub path-SHA guarded Contents writes against the still-current source blob. Production source landed as `a207c0c87f23800e39cbd8aefd818bfb9f2ef2df`; focused regression landed as `79cc183f255d9b0be4ba2fb90a470c243a298828`.
- Remote `main` readback after integration confirms source blob `bdbed57801c7bdc760431295ec3ce4464c9641fc` and smoke blob `10df8551419a7eeebf6822e9ddaae8bae276f354` are present.

## Validation actually performed

- Reviewed exact feature diff: production change only augments `ResolveSelection(...)` with selected-instance capture, post-enumeration identity revalidation and removal/rebind fail-closed behavior.
- Focused smoke covers removal after first yield, same-id object replacement after first yield, and stable lazy Group/Detail selection semantics.
- Final feature branch head checks found no GitHub Actions workflow run on the reviewed patch head; no Actions were dispatched by this lane.
- No local .NET build/smoke execution PASS is claimed from this connector-only lane.
- No licensed BricsCAD V25/V26 runtime PASS or release qualification is claimed.

## Excluded scope honored

`ReportingProjectIdentityGuard.cs`, report-row readonly semantics, quantity math, source-handle resolution, Room Finish lifecycle policy, UI/native/runtime and LOCAL_ONLY qualification were not changed.

## Completion condition

Satisfied. The focused source fix and regression are present on current `main`, remote readback is complete, exact integration evidence is recorded, and this reservation is released.
