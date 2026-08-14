# Agent work claim — ProjectFloorService.Update failure atomicity

- Agent: `chatgpt-56sol`
- Date: 2026-08-14
- Status: `ACTIVE`
- Baseline main SHA: `a69e9a34da00f96d495463412795d8348db10c13`
- Implementation branch: `agent/chatgpt-56sol/floor-update-failure-atomicity`
- Planned integration branch: `integration/chatgpt-floor-update-atomicity-20260814`

## Reserved scope

Fix one confirmed remote-safe Core failure-atomicity defect in `ProjectFloorService.Update`: the service-level `Required(...)` helper accepts control characters, while `FloorDefinition.Name` rejects them. A rejected rename can therefore reach `project.Touch()` and only then throw at `floor.Name = normalizedName`, leaving `ProjectState.ChangeVersion` / `UpdatedUtc` mutated despite the failed operation.

## Expected surfaces

- `src/QS3D.Core/Domain/ProjectFloorService.cs` — align service-level required text validation with the domain edge by rejecting control characters before any project mutation.
- `tests/QS3D.Core.SmokeTests/ProjectFloorUpdateFailureAtomicitySmoke.cs` — focused deterministic `[ModuleInitializer]` smoke proving invalid control-character rename leaves floor and project state unchanged.
- this claim file for final closeout evidence.

## Excluded scope

- `src/QS3D.Core/Domain/ProjectZoneService.cs` and `tests/QS3D.Core.SmokeTests/ProjectZoneUpdateFailureAtomicitySmoke.cs`; the Zone sibling lane landed separately at `a69e9a34da00f96d495463412795d8348db10c13`.
- all other product source/test/script/runtime files unless this claim is amended first after refreshed ownership checks.
- every surface reserved by another `ACTIVE` or `BLOCKED` claim.
- LOCAL_ONLY BricsCAD runtime qualification, private-DWG evidence, signing, packaging, and licensed host validation.
- manual GitHub Actions dispatch/rerun/cancel under `CI_POLICY.md`.

## Validation plan

- refresh current `main` and ownership after publishing this claim;
- create the implementation branch only after the claim is reachable from `main`;
- keep the source fix limited to pre-mutation input validation;
- add a focused deterministic smoke capturing floor name, project `ChangeVersion`, `UpdatedUtc`, active floor, and floor count before a control-character rename, then assert all remain unchanged after rejection;
- inspect the resulting branch diff and compile semantics statically; do not claim managed/native PASS without an executable checkout/runner;
- reconcile onto refreshed `main` through `integration/chatgpt-floor-update-atomicity-20260814`, perform one integration-relevant source/test landing to `main`, verify ancestry/readback, then close this claim `COMPLETED` in a docs-only commit.

## Evidence before registration

At baseline `a69e9a34da00f96d495463412795d8348db10c13`, `ProjectFloorService.Update` computes `normalizedName = Required(...)`, performs other validation, then later calls `project.Touch()` before assigning `floor.Name = normalizedName`. The local `Required(...)` checks only trimmed length. `FloorDefinition.Name` uses the stricter domain `Required(...)` that rejects control characters. The already-landed Zone sibling fix confirms this validation ordering is treated as a failure-atomicity defect, but that lane owns different source/test files.

## Completion condition

The exact Floor source fix and focused regression are reachable from refreshed `main` through the required agent/integration flow; no unrelated source is modified; remote ancestry/readback is verified; validation limitations are recorded without fabricating runtime PASS; this claim is then marked `COMPLETED` with claim/source/regression/integration/main SHAs.
