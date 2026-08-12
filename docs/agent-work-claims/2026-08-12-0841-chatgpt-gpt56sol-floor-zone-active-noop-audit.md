# Work claim — Floor/Zone active canonical no-op audit suppression

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-gpt56sol-20260812-floor-zone-active-noop-audit`
- Registered: `2026-08-12T08:41:00+07:00`
- Last Updated: `2026-08-12T08:46:00+07:00`
- Baseline main SHA: `d898b7ba2e20e979a105b2781145e1eba45bb67c`
- Priority: deterministic shared V25/V26 UI revision/audit mismatch found during owner-requested `continue all`
- Task Key: `UI-FLOOR-ZONE-ACTIVE-CANONICAL-NOOP-AUDIT`
- Implementation PR: `#660`
- Main integration commit: `70abee9dba821ae4564aa2fcfae230ffdb1ad8db`

## Confirmed defect

`ProjectFloorService.SetActive(...)` and `ProjectZoneService.SetActive(...)` deliberately implement the established trimmed case-insensitive semantic no-op contract: if the persisted active id is canonical-equivalent to the requested project-owned id (for example `" F1 "` vs `F1`, or case-only variants), the service returns without touching or rewriting the project. This contract was explicitly restored by `fix(core): restore Floor Zone canonical no-op contract` (`0ce741622c31fe794aa3784ac45c304309d8c2a4`).

The shared V25 UI wrappers previously decided whether to append `floor.activate` / `zone.activate` by comparing the raw pre-service active-id string directly with the canonical selected id. For a canonical-equivalent raw alias, the domain service performed a true no-op but the UI still appended an audit event; `AuditTrail.Record(...)` then advanced `ProjectState.ChangeVersion` and created misleading mutation history.

## Implemented scope

Both activate handlers now:

- preserve the raw `previous` active id for real-mutation audit detail;
- capture `project.ChangeVersion` immediately before the corresponding domain `SetActive(...)` call;
- append the activate audit only when the service advances that version.

No active id is rewritten during a semantic no-op. Rollback snapshots, UI refresh/status behavior and domain target resolution remain unchanged.

## Static regression

Added `scripts/preflight-floor-zone-active-noop-audit.py`, which requires both handlers to preserve previous-id detail, capture `ChangeVersion` before `SetActive`, gate audit on post-service version change, reject reintroduction of raw-id audit decisions, and confirms V26 continues to linked-compile the corrected V25 UI source.

## Coordination / exclusions preserved

- `ProjectFloorService` / `ProjectZoneService` domain semantics were not modified.
- Family active handling, Floor/Zone create/update/delete/assign behavior, persistence schemas and other WPF/native CAD surfaces were not modified.
- Concurrent ProjectElement/Start Center work did not overlap these UI files.
- No force-push, GitHub Actions/build/release dispatch or licensed V25/V26 runtime qualification was performed.

## Validation evidence

- Claim registered on `main` before source edits at `232dfe41ee4e43b3ce215dabc89da46340c30b2b`.
- Post-claim source readback confirmed original Floor blob `5499ae52ca17df26df5d7008393bae2e3f888fde` and Zone blob `ba96f59a7c7db25ec29a20e442a61f6a240dc9b0` before implementation.
- Zone source commit: `a943829798e0c9412bb840e24b7ea6ef72605455`.
- Floor source commit: `38cfc77a79efdcfc3f646df3442f7e01c0037d43`.
- Static preflight/head commit: `44a69158cba8f230a0cc701c1f93207591d65d3d`.
- PR #660 exact diff contained exactly three files, `+77/-2`; the production hunks only added two `beforeVersion` captures and replaced two raw-id audit conditions with version guards.
- Server-side squash merge with exact expected head produced `70abee9dba821ae4564aa2fcfae230ffdb1ad8db` without force.
- Post-merge readback confirms Floor blob `9052fbed0c37e5251b655e7a637a760164c1c73e` and Zone blob `f3838e655b634a2bcdc077698dbcfc397ebc4918` contain the intended guards.
- The static preflight was committed but not executed in this connector-only environment. No WPF/.NET build or licensed BricsCAD V25/V26 runtime PASS is claimed.

## Completion

`COMPLETED`: current `main` no longer creates Floor/Zone activate audit/revision mutations when the domain service treats a canonical-equivalent active id as a true no-op, while real activation changes retain existing audit detail and shared V25/V26 behavior.