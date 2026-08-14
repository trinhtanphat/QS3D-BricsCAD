# Agent work claim — ProjectFamilyService.Rename failure atomicity

- Agent: `chatgpt-56sol-family-rename-atomicity`
- Date: 2026-08-14
- Status: `COMPLETED`
- Baseline main SHA: `472a35fddef701c9d39ea230b17a4abb24c95019`
- Claim commit: `c199efae1995c0e407e416f0370325d62114ca14`
- Implementation branch: `agent/chatgpt-56sol/family-rename-failure-atomicity`
- Source commit: `8930d9265de6170b1c0f8e7314435cc6ddbb1af3`
- Regression commit: `305655df40f10c32d2f80745ad64f1a69f4f92bf`
- Integration branch: `integration/chatgpt-family-rename-atomicity-20260814`
- Implementation integration PR: `#1308`
- Integration merge SHA: `2205de6b732a19dfd64bcce957a317ec7d0627e2`
- Main integration PR: `#1309`
- Main landing SHA: `e8906095bf73c0c8e60423fa02527d97847d4ca3`

## Reserved scope

Fixed one confirmed remote-safe Core failure-atomicity defect in `ProjectFamilyService.Rename`: the service-level `Required(...)` helper accepted control characters, while `ProjectFamily.Name` rejects them. A rejected rename could therefore pass service validation, execute `project.Touch()`, and only then throw at `family.Name = normalized`, leaving `ProjectState.ChangeVersion` / `UpdatedUtc` mutated despite the failed operation.

## Completed surfaces

- `src/QS3D.Core/Domain/ProjectFamilyService.cs` — service-level `Required(...)` now rejects control characters before any Family service mutation can cross the stricter domain edge.
- `tests/QS3D.Core.SmokeTests/ProjectFamilyRenameFailureAtomicitySmoke.cs` — focused deterministic `[ModuleInitializer]` smoke captures Family name, project `ChangeVersion`, `UpdatedUtc`, and Family count, rejects a control-character rename, and asserts all captured state remains unchanged.
- this claim file — final closeout evidence.

## Excluded scope preserved

- `src/QS3D.Core/Domain/ProjectFloorService.cs` / Floor failure-atomicity lane remained independently owned and was not modified by this lane.
- `src/QS3D.Core/Domain/ProjectZoneService.cs` / Zone sibling lane was not modified.
- no unrelated Family assignment/property/duplicate/material/relation behavior was edited beyond the shared service required-text guard.
- no LOCAL_ONLY BricsCAD runtime qualification, native V25/V26 source, private-DWG evidence, signing, packaging, or licensed host validation was claimed.
- no manual GitHub Actions dispatch/rerun/cancel was performed.

## Validation evidence

- Claim-only registration was reachable from `main` at `c199efae1995c0e407e416f0370325d62114ca14` before source work.
- Implementation branch compare against the claim baseline showed exactly two changed files: one added validation line in `ProjectFamilyService.cs` and one new focused smoke regression.
- Concurrent `main` changes after the claim touched Floor/release project files, not `ProjectFamilyService.cs`; the implementation was therefore integrated without overwriting concurrent source.
- PR `#1308` merged the implementation branch into refreshed integration branch at `2205de6b732a19dfd64bcce957a317ec7d0627e2`.
- Immediately before the final landing, refreshed `main` was `6f163b9d031593830153f7345f95b75decc9bd45`; integration compare against that SHA contained only the intended two files.
- PR `#1309` landed the integration branch on `main` at `e8906095bf73c0c8e60423fa02527d97847d4ca3`.
- Remote readback on `main` confirms the control-character guard and the complete focused regression file are present at the landed blobs.
- This environment does not provide an executable QS3D managed/native toolchain, so managed smoke/native BricsCAD PASS is **NOT_RUN / NOT_CLAIMED**. Static source/test review and GitHub remote ancestry/readback are the validation evidence for this lane.

## Completion

`COMPLETED`: the Family rename failure-atomicity defect is fixed with focused regression, integrated through the required agent/integration flow, landed on `main`, remotely read back, and no unrelated source was overwritten.