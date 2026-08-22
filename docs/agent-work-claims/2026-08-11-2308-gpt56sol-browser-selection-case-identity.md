# Work claim — Project Browser selection case-insensitive semantic identity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-browser-selection-case-20260811-2308`
- Registered: `2026-08-11T23:08:00+07:00`
- Completed: `2026-08-11T23:11:00+07:00`
- Baseline main SHA: `d52b4bdd30ec2710708062ebeffcb1133fa3c053`
- Claim commit: `df0df09f65cb9e1da1f20d749984dea4111a548c`
- Claim clarification commit: `c0f219f983883b4d2b22d8dbee701ea99c5e7f17`
- Source fix commit: `80b808080ebc37a59f385dec5ed77d60a257dc6a`
- Regression commit: `836384665180b749706f96d1a8aab35427e518ff`
- Priority: P2 source-proven regression hardening

## Reserved scope

Fix the Project Browser selection reveal identity mismatch where the tree membership index, duplicate detection, and primary-selection validation all use case-insensitive semantic Element IDs, but `PlanReveal` performed its initial root membership check with the default case-sensitive `IReadOnlyList.Contains`. A valid selection such as `b-001` could therefore be rejected as missing when the canonical tree contains `B-001`.

## Implemented surfaces

- `src/QS3D.Core/Navigation/ProjectBrowserSelectionPlanner.cs`
- `tests/QS3D.Core.SmokeTests/ProjectBrowserSelectionPlannerSmoke.cs`
- this claim file

## Implemented fix

- The initial root membership precheck now uses `StringComparer.OrdinalIgnoreCase`, matching the tree membership index, duplicate detection, and primary-selection identity semantics.
- Existing selected/primary caller-casing behavior remains unchanged; this batch changes semantic membership only.
- Focused regression selects `b-001` with primary `B-001` against a tree containing canonical `B-001` and verifies reveal/ancestor planning succeeds deterministically.

## Explicit exclusions honored

- No Project Browser WPF/native/runtime changes.
- No workspace persistence/schema changes.
- No changes to ProjectBrowserPlanner grouping, query, or virtualization semantics.
- No caller-output casing redesign.
- No BricsCAD runtime/local gate changes.
- No GitHub Actions dispatch or workflow edits.

## Validation actually performed

- Claim was committed separately and verified as current `main` before substantive writes.
- Re-fetched exact current source/test blobs and used blob SHA checks for conflict-safe writes.
- Re-read current `main` after implementation and verified `Contains(elementId, StringComparer.OrdinalIgnoreCase)` plus `CaseInsensitiveSelectionIdentityReveals()` are present in the already-registered smoke suite.
- No force push/reset was used.
- No local checkout/.NET build/Core smoke execution was available in this connector-only lane; executable PASS is not claimed.
- No BricsCAD V25 runtime or GitHub Actions execution is claimed.

## Coordination

Recent Project Browser implementation commits are earlier completed work. This batch remained Core selection identity only and did not touch LOCAL-012 native browser qualification.

## Completion condition

Completed. Case-varied semantic selections accepted by the project identity model are no longer rejected by the browser reveal precheck, focused regression coverage is committed on `main`, current source was re-read, and this claim records exact SHAs and the actual validation boundary.
