# Work claim — Floor/Zone assignment canonical no-op audit suppression

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-gpt56sol-20260812-floor-zone-assign-noop-audit`
- Registered: `2026-08-12T08:49:00+07:00`
- Last Updated: `2026-08-12T09:04:00+07:00`
- Baseline main SHA: `6e56c1a9fdd9bd673ebf852393c39d1b6a854d30`
- Priority: deterministic shared V25/V26 UI revision/audit mismatch found during owner-requested `continue all`
- Task Key: `UI-FLOOR-ZONE-ASSIGN-CANONICAL-NOOP-AUDIT`
- Implementation PR: `#667`
- Main implementation commit: `3d7d156d74f8639d64b3ab1cdd53eca5f08ef4da`
- Cleanup PR: `#672`
- Main cleanup commit: `661eb9c07896bc773404c265c44b957a24f2268f`

## Confirmed defect

`ProjectFloorService.Assign(...)` and `ProjectZoneService.Assign(...)` compare existing relation ids after trimming and case-folding. An element whose raw relation is canonical-equivalent to the selected target (for example `" F1 "` vs `F1`) is therefore a true assignment no-op: the domain service returns without changing that element.

The shared V25 UI wrappers previously precomputed audit candidates before the service using raw relation comparisons without trimming. `FloorLevelWindow.OnAssignClick(...)` built `changedElements` from raw `FloorId`; `ZoneManagerWindow.OnAssignClick(...)` built `previous` only for raw `ZoneId` values that differed from the selected canonical id. A canonical-equivalent alias could therefore enter the UI audit set even though the domain service left it untouched. The UI then recorded misleading `floor.assign` / `zone.assign` events and `AuditTrail.Record(...)` advanced `ProjectState.ChangeVersion` for elements that were not mutated.

V26 linked-compiles the V25 UI sources, so the shared-source correction applies to both supported hosts.

## Implemented scope

Both assignment handlers now snapshot every resolved element's raw relation value before the domain service call. After the existing `Assign(...)` returns, the handler appends an audit entry only when the same element's raw relation actually changed by exact ordinal comparison.

This preserves:

- the domain service's trimmed/case-insensitive canonical no-op semantics;
- the domain `changed` return count used in UI status;
- selection/stale-project preflight and ownership checks;
- rollback behavior;
- existing audit action/detail for real relation mutations;
- UI refresh/status behavior;
- the completed Floor/Zone activate no-op audit behavior.

No canonical rewrite of padded/case-varied relation ids was introduced.

## Static regression

Added `scripts/preflight-floor-zone-assign-noop-audit.py`, which requires both handlers to:

- snapshot all raw relation values before the service;
- call the correct domain `Assign(...)`;
- gate each audit on an actual before-vs-after relation-string change;
- reject the old pre-service raw-target filtering patterns;
- preserve V26 linked-source parity through the shared V25 C# source contract.

## Integration / cleanup evidence

- Claim registration was committed to `main` before source edits at `051147d79343f5dac7d76e0be8561a341de998e4`.
- Implementation branch started from moving-main commit `ccbcf8aa6c832b53623320eba4b90edb4cc89d21`, preserving unrelated Floor UI work already merged by PR #662.
- Floor implementation commit: `78725b0d5faffe1e0805627aef03b5434961246a`.
- Zone implementation commit: `7c94bc33d1141037d874f3d3f78cb0b6c1f58067`.
- Static preflight/head commit: `b78bc0aa36d01821add7222a317bfab137a42958`.
- Baseline compare showed exactly three changed files: the new preflight, Floor `+10/-8`, Zone `+7/-5`.
- PR #667 merged the functional change to `main` at `3d7d156d74f8639d64b3ab1cdd53eca5f08ef4da`.
- Exact PR diff review found two unrelated contents-API replacement drifts: a one-line Floor message wording change and missing trailing newline in both UI files. These were deliberately not left as part of the functional change.
- Direct cleanup fast-forward attempts were safely rejected because `main` moved concurrently; no force-push was used.
- A fresh cleanup branch from post-#667 `main` produced PR #672 with exactly two files and `+3/-3`, restoring only the original Floor message wording and normal EOF newlines.
- PR #672 squash-merged at `661eb9c07896bc773404c265c44b957a24f2268f`.
- Post-cleanup `main` readback confirms Floor blob `47f0be199c4802156f0a57e85e0cf7046e4acb59` and Zone blob `b794dcfc49ac8262cfdef7946b9fdd7aa3e7f199` contain the intended before/after audit guards with the unrelated drift removed.
- `scripts/preflight-floor-zone-assign-noop-audit.py` is present on `main` as blob `7d317f021fb943036284a1ef643620db54858559`.

## Coordination / exclusions preserved

- `ProjectFloorService` / `ProjectZoneService` domain assignment semantics were not modified.
- Floor/Zone activate behavior from PR #660 was preserved.
- Family assignment, create/update/delete, native CAD movement, persistence schemas and other WPF windows were not modified.
- No force-push, GitHub Actions/build/release dispatch or licensed BricsCAD V25/V26 runtime qualification was performed.
- The static preflight was committed but not executed in this connector-only environment; no WPF/.NET or licensed BricsCAD runtime PASS is claimed.

## Completion

`COMPLETED`: current `main` records Floor/Zone assignment audit entries only for elements whose relation string was actually mutated by the domain service, while canonical-equivalent assignment no-ops remain audit/revision no-ops and shared V25/V26 behavior is preserved.