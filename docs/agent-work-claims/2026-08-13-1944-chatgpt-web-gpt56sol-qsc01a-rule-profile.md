# Work claim — QSC-01A declarative QS rule profile foundation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-qsc01a-rule-profile-20260813-1944`
- Registered: `2026-08-13T19:44:00+07:00`
- Baseline main SHA: `c24832e32a053f2fe8bcf260e7ffa8ce55f6dd9c`
- Priority: `QSC-01 / P2` — add the smallest declarative QS rule/profile contract on top of existing Semantic Health without creating a second validation engine

## Confirmed source gap

Current `ModelHealthIssue` exposes only health `Code`, `Severity`, `Message`, and `ElementId`, while `ModelHealthService` and category-specific health services emit those issues directly. Current source/history contains no declarative QS rule/profile contract that can give stable rule identity, configured severity, explanation, deterministic profile membership, and an explicit mapping back to existing health issue codes. The QSC workstream explicitly requires this layer to build on existing Semantic Health rather than duplicate its validation logic.

## Reserved scope

- new `src/QS3D.Core/Diagnostics/QsRuleProfile.cs`
- new `tests/QS3D.Core.SmokeTests/QsRuleProfileSmoke.cs`
- new `tests/QS3D.Core.SmokeTests/QsRuleProfileRegistration.cs`
- this claim file

## Intended bounded change

- add immutable `QsRuleDefinition` metadata with stable rule id, existing health issue code, configured severity, and human explanation;
- add immutable/detached `QsRuleProfile` with stable profile id, deterministic rule ordering, read-only snapshot semantics, duplicate rule-id rejection, and ambiguous duplicate health-code rejection;
- resolve a `ModelHealthIssue` to profile metadata strictly by its existing issue code; unmapped health issues remain explicitly unmapped;
- validate malformed identities, undefined severity, blank/control-character explanations, null collections, and null rule entries visibly;
- add focused smoke regressions for ordering/detachment/read-only behavior, mapped/unmapped resolution, duplicate ambiguity, and malformed input.

## Excluded scope

- no edits to `ModelHealthService`, existing health services, `ModelHealthIssue`, or health-check business logic;
- no new predicate/condition evaluator, no parallel model-validation engine, and no mutation of `ProjectState`;
- no QSC-02 high-value rule families, QSC-03 autofix/preview, UI/report rendering, persistence/schema, MAP, CST, native BricsCAD, or cross-repo platform work;
- no GitHub Actions dispatch, force-push, or unexecuted managed/native PASS claim.

## Completion condition

Claim-only reservation is published first; after overlap recheck, the new Core profile contract plus focused registered smoke are committed on current `main`, exact remote files are read back/reconciled, and this claim is closed `COMPLETED` with only validation actually executed recorded.