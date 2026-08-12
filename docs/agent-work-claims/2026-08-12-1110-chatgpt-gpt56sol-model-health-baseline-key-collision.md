# Work claim — Model health baseline key collision

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-gpt56sol-model-health-baseline-key-collision-20260812`
- Registered: `2026-08-12T11:10:00+07:00`
- Completed: `2026-08-12T11:12:00+07:00`
- Baseline main SHA: `bb2337087efb7bc1f80213d78073721e727b4c52`
- Task Key: `CORE-MODEL-HEALTH-BASELINE-KEY-COLLISION`

## Defect

`ModelHealthBaselineService.Key` concatenated severity, code, element id and ordinary issue message with newline separators. `ModelHealthIssue` permits embedded newlines, so distinct diagnostic fields could produce the same key. Baseline capture could drop an issue and baseline compare could classify a changed issue as persistent instead of one resolved plus one new.

## Completed change

Issue identity fields are now encoded with invariant length-prefixing before concatenation. This preserves severity sensitivity, case-insensitive code/element identity, exact ordinary-message identity and the existing `*_STALE` message-insensitive identity. Existing malformed-issue validation and deterministic sorting are unchanged.

## Regression evidence

`tests/QS3D.Core.SmokeTests/ModelHealthBaselineStructuralIdentitySmoke.cs` proves:

- two issues that collided under the old newline-delimited key both survive Capture;
- Compare reports one new and one resolved issue rather than one persistent issue;
- case-only code/element changes plus message changes for `*_STALE` remain persistent.

## Integration evidence

- Claim registration: `c300c2db59663b11961fa1b49418d504e763aa58`.
- Source fix branch commit: `e781f4fc0f1e03bb468007eacb72cfe7d9ee64f5`.
- Focused smoke branch commit: `53b4dbc4da5468e22657ae4da0848447fe21b037`.
- Pull Request: `#807`.
- Squash merge: `5425f964c3179f2efad8d800a2a1b1075006b4ea`.
- Main source readback blob: `0b40aa5ecd6fa58d5c1fb6d6f7d007fa271bdcdb`.
- Main smoke readback blob: `b826630351389a63d4255f084b130763ae014adc`.
- Ancestry verification: `main` was ahead by 2, behind by 0, merge base exactly `5425f964c3179f2efad8d800a2a1b1075006b4ea`.

## Validation boundary

No GitHub Actions, full build, executable smoke or licensed BricsCAD V25/V26 runtime PASS was executed or claimed in this connector session.
