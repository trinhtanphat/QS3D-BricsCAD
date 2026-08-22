# Work claim — Quantity Rule engine canonical element ownership

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-quantity-rule-engine-ownership-20260811-2326`
- Registered: `2026-08-11T23:26:00+07:00`
- Completed: `2026-08-11T23:30:00+07:00`
- Baseline main SHA: `53f9e9c42aad75a59a7dc3c713e3928f989d5e15`
- Priority: P0 Core mutation-boundary correctness during owner-requested `continue all`

## Confirmed defect

`QuantityRuleEngine.ApplyMatching(ProjectState, ProjectElement)` was a public mutation API but did not verify that the supplied element was the exact canonical instance owned by the supplied project. A detached/stale element with the same ID could therefore resolve rules/family metadata from the canonical project while receiving quantity/provenance mutations outside project state. `QuantityRulePreviewService` already enforced the exact `ReferenceEquals(project.FindElement(id), element)` contract before applying rules, so the engine boundary was the lower-level gap.

The existing rule application algorithm already staged all rule evaluations before quantity/provenance writes; this claim did not change dependency ordering or atomic evaluation behavior.

## Reserved scope

- `src/QS3D.Core/Rules/QuantityRuleEngine.cs`
- `tests/QS3D.Core.SmokeTests/QuantityRuleEngineOwnershipRegressionSmoke.cs`
- this claim file for close-out

## Implemented contract

- `ApplyMatching` now requires the supplied element to be the exact canonical project-owned instance;
- a detached element with a colliding ID fails closed before stale-output cleanup, formula evaluation or any mutation;
- an element owned by a different project fails closed;
- a canonical element continues to apply matching rules normally;
- an independently reconstructed project with its own canonical element remains valid;
- `Apply(ProjectElement, QuantityRule, variables)` remains the intentionally project-agnostic primitive and was not changed.

## Commits

- Production: `4d6949000146653efec87612ea4738d261a14282` — `fix(quantity): require canonical rule target ownership`.
- Regression: `5364d543ce9115b24f54b7727ea3b3797a14e701` — `test(quantity): guard rule target ownership`.

## Validation performed

- Re-fetched current `main` after concurrent updates and confirmed the exact `ReferenceEquals(project.FindElement(element.Id), element)` guard remains before rule lookup/stale cleanup.
- Re-fetched the focused smoke and confirmed coverage for canonical success, detached same-ID rejection with old managed quantity/provenance preserved, cross-project rejection, and independent canonical-project success.
- Existing category validation and staged rule evaluation remain present.
- No GitHub Actions were dispatched; no BricsCAD V25 runtime PASS is claimed from this remote lane.

## Coordination

- prior `quantity-rule-category-integrity` claim was `COMPLETED` before this lane started;
- active Quantity Settings UI work reserved disjoint XAML/code-behind surfaces and explicitly excluded Core quantity models/arithmetic;
- no Quantity Settings UI/store, native commands, persistence, reporting, updater, rebar, or other active lane was modified.

## Completion condition

Satisfied. The Core quantity-rule mutation boundary rejects non-canonical elements before mutation, focused behavioral regression is present on current `main`, and the production/test SHAs are recorded above.
