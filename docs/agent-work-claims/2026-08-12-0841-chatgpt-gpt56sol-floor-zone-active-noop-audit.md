# Work claim — Floor/Zone active canonical no-op audit suppression

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-floor-zone-active-noop-audit`
- Registered: `2026-08-12T08:41:00+07:00`
- Last Updated: `2026-08-12T08:41:00+07:00`
- Baseline main SHA: `d898b7ba2e20e979a105b2781145e1eba45bb67c`
- Priority: deterministic shared V25/V26 UI revision/audit mismatch found during owner-requested `continue all`
- Task Key: `UI-FLOOR-ZONE-ACTIVE-CANONICAL-NOOP-AUDIT`

## Confirmed defect

`ProjectFloorService.SetActive(...)` and `ProjectZoneService.SetActive(...)` deliberately implement the established trimmed case-insensitive semantic no-op contract: if the persisted active id is canonical-equivalent to the requested project-owned id (for example `" F1 "` vs `F1`, or case-only variants), the service returns without touching or rewriting the project. This contract was explicitly restored on current history by `fix(core): restore Floor Zone canonical no-op contract` (`0ce741622c31fe794aa3784ac45c304309d8c2a4`).

The shared V25 UI wrappers do not use that service mutation result as the audit boundary. `FloorLevelWindow.OnActivateClick(...)` and `ZoneManagerWindow.OnActivateClick(...)` capture the raw active-id string before `SetActive(...)`, then decide whether to append `floor.activate` / `zone.activate` by comparing that raw string directly to the canonical selected id without trimming. For a canonical-equivalent raw alias, the domain service performs a true no-op but the UI still appends an audit event; `AuditTrail.Record(...)` then advances `ProjectState.ChangeVersion` and creates misleading mutation history.

`FamilyManagerWindow.Active.cs` does not have this defect because it resolves the previous active Family to a canonical `ProjectFamily` before comparison.

V26 linked-compiles V25 C# source, so this shared-source correction applies to both supported plugin hosts.

## Reserved scope

In Floor and Zone `OnActivateClick(...)`, capture `project.ChangeVersion` immediately before the corresponding domain `SetActive(...)` call and append the activate audit event only when that service advances the version. Preserve the existing raw previous id for real-mutation audit detail, rollback snapshots, UI refresh/status text, domain canonical no-op behavior and project-owned target resolution.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/FloorLevelWindow.xaml.cs`
- `src/QS3D.BricsCAD.V25/UI/ZoneManagerWindow.xaml.cs`
- one focused static preflight under `scripts/` pinning both mutation-only audit guards and V26 linked-source parity
- this claim file

## Excluded scope

- `ProjectFloorService` / `ProjectZoneService` domain semantics, including the restored canonical no-op contract.
- Family active handling, Floor/Zone create/update/delete/assign behavior, persistence schemas, other WPF windows, native CAD operations.
- any attempt to rewrite padded/case-varied active ids during an otherwise semantic no-op.
- GitHub Actions/build/release dispatch or licensed BricsCAD V25/V26 runtime qualification.

## Validation plan

- Static preflight requires pre-service `ChangeVersion` capture, the correct domain `SetActive(...)` call, a post-service version guard and the expected activate audit record for both Floor and Zone handlers.
- Preserve the raw `previous` value only for real-mutation audit detail; do not reintroduce raw-id comparison as the mutation decision.
- Verify V26 continues to linked-compile the corrected V25 UI source.
- Re-fetch moving `main` and exact Floor/Zone UI blobs after claim publication and immediately before PR integration; review exact diff before merge.
- No executable WPF/native runtime PASS is claimed remotely.

## Coordination

At registration time open PR #656 owns `ProjectElement.Category` invalidation only and does not overlap these UI files. No discovered current claim owns Floor/Zone active audit semantics. Earlier active-id canonicalization/revert work is completed and is treated as the domain contract this wrapper fix must preserve.

## Completion condition

Current `main` no longer creates Floor/Zone activate audit/revision mutations when the domain service treats a canonical-equivalent active id as a true no-op, while real activation changes retain existing audit detail and shared V25/V26 UI behavior.