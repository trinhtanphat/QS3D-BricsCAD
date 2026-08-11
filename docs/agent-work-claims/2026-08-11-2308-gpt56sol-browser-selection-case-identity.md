# Work claim — Project Browser selection case-insensitive semantic identity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-browser-selection-case-20260811-2308`
- Registered: `2026-08-11T23:08:00+07:00`
- Baseline main SHA: `d52b4bdd30ec2710708062ebeffcb1133fa3c053`
- Priority: P2 source-proven regression hardening

## Reserved scope

Fix the Project Browser selection reveal identity mismatch where the tree membership index, duplicate detection, and primary-selection validation all use case-insensitive semantic Element IDs, but `PlanReveal` performs its initial root membership check with the default case-sensitive `IReadOnlyList.Contains`. A valid selection such as `b-001` can therefore be rejected as missing when the canonical tree contains `B-001`.

## Expected surfaces

- `src/QS3D.Core/Navigation/ProjectBrowserSelectionPlanner.cs`
- `tests/QS3D.Core.SmokeTests/ProjectBrowserSelectionPlannerSmoke.cs`
- this claim file for close-out

## Explicit exclusions

- No Project Browser WPF/native/runtime changes.
- No workspace persistence/schema changes.
- No changes to ProjectBrowserPlanner grouping, query, or virtualization semantics.
- No BricsCAD runtime/local gate changes.
- No GitHub Actions dispatch or workflow edits.

## Validation plan

- Verify claim reachability from current `main`, then re-fetch exact source/test blobs before implementation.
- Make the initial root membership check use the same case-insensitive identity semantics already used by the planner membership index and primary selection.
- Preserve canonical output IDs from the tree rather than returning caller casing where the existing planner already resolves canonical membership.
- Add focused smoke coverage selecting a canonical tree element with different casing and verifying reveal/primary/target paths succeed deterministically.
- Source/static readback plus committed smoke coverage only; no local .NET/BricsCAD/Actions PASS claim.

## Coordination

Recent Project Browser implementation commits are from earlier completed work; no recent active browser-selection claim appears in commit history. This lane is Core selection identity only and does not touch LOCAL-012 native browser qualification.

## Completion condition

Case-varied semantic selections accepted by the project identity model are no longer rejected by the browser reveal precheck, focused regression coverage is committed on `main`, current source is re-read, and this claim is marked `COMPLETED` with exact SHAs and actual validation scope.
