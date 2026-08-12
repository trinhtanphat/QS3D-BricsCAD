# Work claim — Floor/Zone assignment canonical no-op audit suppression

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-floor-zone-assign-noop-audit`
- Registered: `2026-08-12T08:49:00+07:00`
- Last Updated: `2026-08-12T08:49:00+07:00`
- Baseline main SHA: `6e56c1a9fdd9bd673ebf852393c39d1b6a854d30`
- Priority: deterministic shared V25/V26 UI revision/audit mismatch found during owner-requested `continue all`
- Task Key: `UI-FLOOR-ZONE-ASSIGN-CANONICAL-NOOP-AUDIT`

## Confirmed defect

`ProjectFloorService.Assign(...)` and `ProjectZoneService.Assign(...)` compare existing relation ids after trimming and case-folding. An element whose raw relation is canonical-equivalent to the selected target (for example `" F1 "` vs `F1`) is therefore a true assignment no-op: the domain service returns without changing that element.

The shared V25 UI wrappers precompute audit candidates before the service using raw relation comparisons without trimming. `FloorLevelWindow.OnAssignClick(...)` builds `changedElements` from raw `FloorId`; `ZoneManagerWindow.OnAssignClick(...)` builds `previous` only for raw `ZoneId` values that differ from the selected canonical id. A canonical-equivalent alias can therefore enter the UI audit set even though the domain service leaves it untouched. After the service returns, the UI records audit entries for those precomputed candidates, causing misleading `floor.assign` / `zone.assign` events and `ProjectState.ChangeVersion` increments for elements that were not mutated.

The completed active-id wrapper lane is separate. V26 linked-compiles the V25 UI sources, so this correction applies to both supported hosts.

## Reserved scope

For Floor and Zone assignment handlers, snapshot each resolved element's raw relation value before the domain service, invoke the existing `Assign(...)`, then append an audit event only when that element's raw relation actually changed after the service. Preserve domain return count, selection/stale-project preflight, rollback, audit detail for real changes, UI status/refresh behavior and the domain canonical no-op contract.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/FloorLevelWindow.xaml.cs`
- `src/QS3D.BricsCAD.V25/UI/ZoneManagerWindow.xaml.cs`
- one focused static preflight under `scripts/` pinning before/after mutation-only assignment audit and V26 linked-source parity
- this claim file

## Excluded scope

- `ProjectFloorService` / `ProjectZoneService` domain assignment semantics.
- Floor/Zone activate behavior completed by PR #660 / `70abee9dba821ae4564aa2fcfae230ffdb1ad8db`.
- Family assignment, create/update/delete, native CAD movement, persistence schemas or other WPF windows.
- Any canonical rewrite of padded/case-varied relation ids during a semantic no-op.
- GitHub Actions/build/release dispatch or licensed BricsCAD V25/V26 runtime qualification.

## Validation plan

- Static preflight requires both handlers to snapshot all resolved element relation values before the service, call the domain `Assign(...)`, and gate each audit on actual before-vs-after relation change.
- Reject the current pre-service raw-target filtering pattern as an audit decision.
- Preserve existing domain changed-count status reporting and exact real-mutation audit text.
- Verify V26 linked-source parity.
- Re-fetch moving `main` and exact UI blobs after claim publication and immediately before integration; current Floor UI contains unrelated recently merged PR #662 content and must be preserved.
- No executable WPF/native runtime PASS is claimed remotely.

## Coordination

At the latest pre-registration check no pull requests were open. Current Floor assignment handler still contains the raw `changedElements` filter after PR #662; current Zone handler still contains the raw prefiltered `previous` dictionary. No discovered current claim owns Floor/Zone assignment audit semantics.

## Completion condition

Current `main` records Floor/Zone assignment audit entries only for elements whose relation string was actually mutated by the domain service, while canonical-equivalent assignment no-ops remain audit/revision no-ops and shared V25/V26 behavior is preserved.