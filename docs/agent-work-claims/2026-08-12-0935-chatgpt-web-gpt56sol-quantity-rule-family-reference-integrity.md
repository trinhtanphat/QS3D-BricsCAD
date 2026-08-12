# Work claim — Quantity Rule family-reference integrity

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T09:35:00+07:00`
- Baseline main SHA: `7415e474fb6d913d70a37b322dd163ac80685124`
- Priority: P1 Core quantity correctness during owner-requested `continue all`

## Confirmed defect

`QuantityRuleEngine.ApplyMatching(...)` requires the exact canonical project-owned Element and validates persisted Quantity Rule identities, but `BuildVariables(...)` currently resolves `element.FamilyId` with `project.FindFamily(...)` and silently skips it when the referenced Family is missing. It also projects numeric Family properties without checking that the resolved Family category matches the Element category. A malformed persisted Element can therefore produce apparently valid managed quantities from incomplete or incompatible semantic state instead of failing closed.

This is inconsistent with other semantic read paths such as `SemanticSelectionInspector`, which reject missing Family references and Element/Family category mismatch before projecting effective properties.

## Reserved scope

- `src/QS3D.Core/Rules/QuantityRuleEngine.cs` — family-reference preflight in rule variable projection only.
- `tests/QS3D.Core.SmokeTests/QuantityRuleFamilyReferenceIntegritySmoke.cs` — focused auto-registered Core smoke.
- this claim file for close-out.

## Functional contract

- blank/no FamilyId remains valid and contributes no Family variables;
- a nonblank FamilyId must resolve to exactly one project Family;
- the resolved Family category must equal the canonical target Element category;
- dangling/mismatched Family state fails closed before stale-output cleanup or generated quantity/provenance mutation;
- valid Family numeric-property projection, instance-property precedence, rule dependency ordering and provenance semantics remain unchanged.

## Coordination

Prior Quantity Rule ownership, null-rule, duplicate-ID and preview global-element integrity claims are `COMPLETED`. Recent active claims reserve Audit, health, snapshot, floor/vertical-placement, interchange, grid, reporting and release-preflight surfaces; none reserve this source scope.

## Validation plan

Add deterministic Core smoke coverage for dangling FamilyId, mismatched Family category, blank FamilyId and valid same-category Family projection. Re-fetch source/claim before each write and verify exact pushed diffs plus ancestry. No GitHub Actions dispatch, executable .NET smoke/build PASS, or licensed BricsCAD V25/V26 runtime qualification will be claimed unless actually executed.
