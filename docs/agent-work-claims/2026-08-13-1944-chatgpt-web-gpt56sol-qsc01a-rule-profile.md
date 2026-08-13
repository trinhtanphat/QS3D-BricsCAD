# Work claim — QSC-01A declarative QS rule profile foundation

- Status: `COMPLETED`
- Agent: `chatgpt-gpt56sol-qsc01a-rule-profile-20260813-2007`
- Registered: `2026-08-13T20:07:00+07:00`
- Completed: `2026-08-13T20:10:00+07:00`
- Baseline main SHA: `ec0617d6a350315b8891bc175e54c863149b3e15`
- Priority: `QSC-01 / P2`

## Coordination

This reactivated the previously released QSC-01A reservation after fresh searches confirmed no competing `QsRuleProfile` implementation. Concurrent Zone/Floor revision, release-125 guards, MAP-01B, CST-04 and local/runtime work stayed disjoint.

## Implemented contract

- Added immutable `QsRuleDefinition` metadata with stable `RuleId`, existing `HealthIssueCode`, configured `HealthSeverity`, and validated explanation.
- Added immutable/detached `QsRuleProfile` with stable profile identity, deterministic rule ordering, read-only snapshot semantics, case-insensitive duplicate rule-id rejection, and ambiguous duplicate health-code rejection.
- Added `TryResolve` / `Resolve` mapping from existing `ModelHealthIssue` strictly through its health issue code; message, element id and runtime issue severity do not create a second predicate engine. Unmapped issues remain explicitly unmapped.
- Added fail-closed input validation for null collections/items, malformed/blank identities, undefined severity, and blank/control-character explanations.
- Existing `ModelHealthService`, health predicates, project state, persistence, UI and native BricsCAD code remain unchanged.

## Regression

`tests/QS3D.Core.SmokeTests/QsRuleProfileSmoke.cs` is registered with `[ModuleInitializer]` and covers deterministic ordering, detached/read-only behavior, mapped/unmapped resolution, case-insensitive identity collisions, duplicate health-code ambiguity, null inputs, malformed identities, invalid severity and invalid explanations.

## Evidence

- reactivation commit on branch: `430ebd1e1546a6d17115b4abc3b1e17242ab4767`
- source commit on branch: `6c6cdd23818d58a1f202221f7eeba71989e47bb6`
- regression commit on branch: `de9cb0fb045d2b770b93a087ff132732781c20fb`
- PR: `#1060`
- squash merge to `main`: `a628cb831125d8d77649d7e491d2ef4ebb09065e`
- source readback blob before merge: `a01423ae30dbeb4f45bb2653b9d5f2944cd621b4`
- smoke readback blob before merge: `1e96b9580d45deb626a4965a41dd7614a8e5a6c2`
- PR changed-files readback: exactly this claim file, `src/QS3D.Core/Diagnostics/QsRuleProfile.cs`, and `tests/QS3D.Core.SmokeTests/QsRuleProfileSmoke.cs`.
- PR head had no status checks/workflow runs; no CI-green claim is made.

## Validation actually performed

Exact GitHub source/test readback, changed-file verification, moving-main comparison, PR mergeability check, and squash merge with expected head SHA. No managed build/smoke process, GitHub Actions, adapter build, packaging or licensed BricsCAD runtime execution was performed; no execution PASS is claimed.

## Completion condition

Satisfied for the bounded QSC-01A Core source/static lane: declarative QS rule/profile metadata now builds on existing Semantic Health without duplicating validation logic, focused registered regression is on `main`, and unavailable execution/native gates remain explicitly unclaimed.