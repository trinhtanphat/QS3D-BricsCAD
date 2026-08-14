# Agent work claim — ProjectFamilyService.Rename failure atomicity

- Agent: `chatgpt-56sol-family-rename-atomicity`
- Date: 2026-08-14
- Status: `ACTIVE`
- Baseline main SHA: `472a35fddef701c9d39ea230b17a4abb24c95019`
- Implementation branch: `agent/chatgpt-56sol/family-rename-failure-atomicity`
- Planned integration branch: `integration/chatgpt-family-rename-atomicity-20260814`

## Reserved scope

Fix one confirmed remote-safe Core failure-atomicity defect in `ProjectFamilyService.Rename`: the service-level `Required(...)` helper accepts control characters, while `ProjectFamily.Name` rejects them. A rejected rename can therefore pass service validation, execute `project.Touch()`, and only then throw at `family.Name = normalized`, leaving `ProjectState.ChangeVersion` / `UpdatedUtc` mutated despite the failed operation.

## Expected surfaces

- `src/QS3D.Core/Domain/ProjectFamilyService.cs` — align rename pre-mutation required-text validation with the `ProjectFamily.Name` domain edge by rejecting control characters before `project.Touch()`.
- `tests/QS3D.Core.SmokeTests/ProjectFamilyRenameFailureAtomicitySmoke.cs` — focused deterministic smoke proving a rejected control-character rename leaves Family and project state unchanged.
- this claim file for final closeout evidence.

## Excluded scope

- `src/QS3D.Core/Domain/ProjectFloorService.cs` / Floor failure-atomicity lane, which is independently ACTIVE.
- `src/QS3D.Core/Domain/ProjectZoneService.cs` / Zone sibling lane.
- all other Family assignment/property/duplicate/material/relation semantics unless this claim is amended after refreshed ownership checks.
- LOCAL_ONLY BricsCAD runtime qualification, native V25/V26 source, private-DWG evidence, signing, packaging, and licensed host validation.
- manual GitHub Actions dispatch/rerun/cancel under `CI_POLICY.md`.

## Validation plan

- verify this claim is reachable from refreshed `main`, then re-read concurrent claim deltas before creating the implementation branch;
- keep production change to pre-mutation validation only;
- add a focused deterministic smoke that captures Family name, project `ChangeVersion`, and `UpdatedUtc`, attempts a control-character rename, verifies rejection, then asserts all captured state is unchanged;
- inspect the branch diff and compile semantics statically; do not claim managed/native PASS without an executable checkout/runner;
- reconcile onto refreshed `main` through the planned integration branch, land source/test through one integration PR/merge, verify ancestry/readback, then close this claim `COMPLETED` with exact evidence.

## Evidence before registration

At baseline `472a35fddef701c9d39ea230b17a4abb24c95019`, `ProjectFamilyService.Rename` computes the new name via its local `Required(...)`, checks uniqueness, then calls `project.Touch()` before assigning `family.Name`. That local `Required(...)` checks only trimmed length. `ProjectFamily.Name` uses `RequireName(...)`, which rejects control characters. Therefore a control-character name can fail after project persistence state has already been mutated.

## Completion condition

The exact Family rename source fix and focused regression are reachable from refreshed `main` through the required agent/integration flow; no unrelated source is modified; remote ancestry/readback is verified; validation limitations are recorded without fabricating runtime PASS; this claim is then marked `COMPLETED` with claim/source/regression/integration/main SHAs.
