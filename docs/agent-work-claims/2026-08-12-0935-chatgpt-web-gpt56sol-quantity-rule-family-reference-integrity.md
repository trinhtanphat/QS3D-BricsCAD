# Work claim — Quantity Rule family-reference integrity

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T09:35:00+07:00`
- Completed: `2026-08-12T09:41:00+07:00`
- Baseline main SHA: `7415e474fb6d913d70a37b322dd163ac80685124`
- Claim commit: `35040ebbdedc4ec0a2d801dfb805d9611a6e1fd4`
- Source fix commit: `342aeb0cba92e3cc1e9d3ec99e558e1f1db18c54`
- Regression smoke commit: `dcd60ea1bd1b3fbce6e901a52a08a5cc6cf8956e`
- Priority: P1 Core quantity correctness during owner-requested `continue all`

## Confirmed defect

`QuantityRuleEngine.ApplyMatching(...)` required the exact canonical project-owned Element and validated persisted Quantity Rule identities, but rule variable projection resolved `element.FamilyId` with `project.FindFamily(...)` and silently skipped a nonblank missing Family. It also projected numeric Family properties without checking that the resolved Family category matched the Element category. A malformed persisted Element could therefore produce apparently valid managed quantities from incomplete or incompatible semantic state instead of failing closed. In the no-active-rule path, stale managed outputs could even be removed before this malformed Family state was observed anywhere.

This was inconsistent with semantic read paths such as `SemanticSelectionInspector`, which reject missing Family references and Element/Family category mismatch before projecting effective properties.

## Implemented contract

- `ApplyMatching(...)` now resolves and validates the target Family immediately after canonical Element/rule-identity preflight and before active-rule/stale-output processing.
- blank/no `FamilyId` remains valid and contributes no Family variables;
- a nonblank `FamilyId` must resolve through canonical `ProjectState.FindFamily(...)`;
- the resolved Family category must equal the canonical target Element category;
- dangling/mismatched Family state fails closed before stale-output cleanup or generated quantity/provenance mutation;
- valid Family numeric-property projection and instance-property precedence are preserved by passing the already validated Family into `BuildVariables(...)`.
- rule dependency ordering, expression evaluation and provenance semantics are unchanged.

## Regression coverage

`QuantityRuleFamilyReferenceIntegritySmoke` is auto-registered with a module initializer and covers:

- dangling FamilyId with no active rules: rejection occurs before stale managed quantity/provenance cleanup;
- Family category mismatch: rejection occurs before managed quantity/provenance creation;
- blank FamilyId remains valid and evaluates the built-in `Count` variable normally;
- valid same-category Family projection retains instance-property precedence (`Factor=3` overrides Family `Factor=2`, so `Factor*2` yields `6`).

## Validation performed

- Exact source commit diff readback confirmed only Family resolution/category validation plus validated-Family projection wiring changed in `QuantityRuleEngine.cs`.
- Focused smoke commit readback confirmed all four deterministic cases above and isolated module-initializer registration.
- Compared source fix `342aeb0cba92e3cc1e9d3ec99e558e1f1db18c54` to observed current `main` `ec9e169a4c0974616be451034da991aa8dd6245c`: `behind_by=0`, and no concurrent commit in that range modified `src/QS3D.Core/Rules/QuantityRuleEngine.cs`.
- No GitHub Actions were dispatched. No executable .NET smoke/full build PASS and no licensed BricsCAD V25/V26 runtime qualification are claimed from this connector-only session.

## Coordination

Prior Quantity Rule ownership, null-rule, duplicate-ID and preview global-element integrity claims were `COMPLETED` before this lane. Concurrent work after the source fix touched disjoint Audit/health/snapshot/floor/interchange/grid/reporting/release and other Core surfaces; no conflicting `QuantityRuleEngine.cs` edit was observed before close-out.

## Completion

`COMPLETED`: Quantity Rule evaluation now fails closed on dangling or category-incompatible Family references before stale cleanup or rule mutation, while valid Family/instance variable precedence remains unchanged.
