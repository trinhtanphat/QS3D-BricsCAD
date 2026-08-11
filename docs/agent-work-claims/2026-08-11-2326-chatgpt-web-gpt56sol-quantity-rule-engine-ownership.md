# Work claim — Quantity Rule engine canonical element ownership

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-rule-engine-ownership-20260811-2326`
- Registered: `2026-08-11T23:26:00+07:00`
- Baseline main SHA: `53f9e9c42aad75a59a7dc3c713e3928f989d5e15`
- Priority: P0 Core mutation-boundary correctness during owner-requested `continue all`

## Confirmed defect

`QuantityRuleEngine.ApplyMatching(ProjectState, ProjectElement)` is a public mutation API but does not verify that the supplied element is the exact canonical instance owned by the supplied project. A detached/stale element with the same ID can therefore resolve rules/family metadata from the canonical project while receiving quantity/provenance mutations outside project state. `QuantityRulePreviewService` already enforces the exact `ReferenceEquals(project.FindElement(id), element)` contract before applying rules, so the engine boundary remains the lower-level gap.

The existing rule application algorithm already stages all rule evaluations before quantity/provenance writes; this claim does not change dependency ordering or atomic evaluation behavior.

## Reserved scope

- `src/QS3D.Core/Rules/QuantityRuleEngine.cs`
- `tests/QS3D.Core.SmokeTests/QuantityRuleEngineOwnershipRegressionSmoke.cs` (new)
- this claim file for close-out

## Functional contract

- `ApplyMatching` requires the supplied element to be the exact canonical project-owned instance;
- a detached element with a colliding ID fails closed before stale-output cleanup, formula evaluation or any mutation;
- an element owned by a different project fails closed;
- a canonical element continues to apply matching rules normally;
- a cloned project with its own canonical cloned element remains valid;
- `Apply(ProjectElement, QuantityRule, variables)` remains the intentionally project-agnostic primitive and is out of scope.

## Coordination

- prior `quantity-rule-category-integrity` claim is `COMPLETED` and no longer reserves `QuantityRuleEngine.cs`;
- active `quantity-rule-create-ui` reserves only Quantity Settings XAML/code-behind and explicitly excludes Core quantity models/arithmetic;
- do not modify Quantity Settings UI/store, native commands, persistence, reporting, updater, rebar, or other active lanes.

## Validation plan

- focused net8 Core smoke proves canonical apply succeeds;
- detached same-ID element is rejected and remains unmodified;
- cross-project element is rejected;
- cloned-project canonical element succeeds;
- re-fetch source/test from current `main` after writes and close with exact SHAs;
- no GitHub Actions dispatch and no BricsCAD V25 runtime PASS claim.

## Completion condition

The Core quantity-rule mutation boundary rejects non-canonical elements before mutation, focused behavioral regression is present on current `main`, and this claim is marked `COMPLETED` with exact implementation/test SHAs and actual validation limits.
