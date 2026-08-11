# Agent Work Claim — ProjectState name invariant

- Status: `RELEASED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11` (UTC+7)
- Released: `2026-08-11` (UTC+7)
- Baseline main SHA: `12e9ecbf3b260dee6a887d6db744b3d4e7d4b85c`

## Confirmed defect

`ProjectState` normalized/guarded the project name in its constructor, but the public `Name` setter was an unguarded auto-property. After construction, callers could assign `null`, whitespace, or an untrimmed value and place the canonical project model in a state the constructor itself rejected/normalized.

## Released scope

- `src/QS3D.Core/Domain/ProjectState.cs` — project-name invariant only.
- `tests/QS3D.Core.SmokeTests/ProjectStateNameInvariantSmoke.cs` — focused regression coverage.
- `tests/QS3D.Core.SmokeTests/ProjectStateNameInvariantSmokeRegistration.cs` — module registration for that smoke only.
- this claim file.

## Completed changes

- Source fix: `65bcb965507cf43775cd4404f3a449ba5f32dbc1`
  - introduced a private `_name` backing field;
  - preserved constructor compatibility where blank names fall back to `QS3D Project`;
  - made later `Name` assignments reject null/blank values;
  - normalized accepted assignments with `Trim()`;
  - validation happens before assignment, so rejected names leave the previous valid value intact.
- Smoke coverage: `8879012c9ba9e00465354643213e0dd0aa9659e1`
  - covers constructor blank fallback;
  - covers setter trimming;
  - covers whitespace/null rejection and state preservation.
- Smoke registration: `41b30b04c5bed23c163ad643798c21cfe0b58d5f`
  - registers the focused smoke via `ModuleInitializer` using the existing smoke-test pattern.

## Coordination / validation boundary

- The work claim was committed to `main` before source changes (`4090f18b4a12062c6143b54f6a8191a472ff5d9e`).
- `main` advanced repeatedly during implementation. Two attempted low-level non-force ref updates were rejected as non-fast-forward; no force push, reset, rebase, or overwrite was used. The final source/test writes used GitHub's contents API against the live branch and exact source blob SHA.
- Current source was re-read immediately before the successful write and still matched the claimed pre-fix blob.
- No GitHub Actions workflow was dispatched.
- This connector environment did not provide a usable local repository/build/runtime environment, so the smoke executable and full solution were not executed here.
- No BricsCAD V25 runtime qualification is claimed.

## Result

The canonical project name can no longer be corrupted by post-construction null/blank assignment, accepted mutations use the same trimming convention as construction, and the legacy blank-constructor fallback remains unchanged.
