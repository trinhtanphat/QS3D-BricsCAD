# Work claim — Semantic sheet auto-layout gap precision

- Status: `COMPLETED`
- Agent: `gpt56sol-auto-layout-gap-precision-20260814-0855`
- Registered: `2026-08-14T08:55:00+07:00`
- Completed: `2026-08-14T09:00:00+07:00`
- Baseline main SHA: `497b1936792fd0194494896128628fc4de08bf15`
- Priority: P1 documentation/layout correctness hardening; positive auto-layout gaps could be lost at large finite coordinates even though sibling margin/schedule placement paths fail closed on the same floating-point precision loss.

## Reserved scope

Harden `SemanticSheetAutoLayoutPlanner.PageState` packing arithmetic so configured positive horizontal/vertical gaps are never silently rounded away. Preserve ordinary packing/pagination behavior and fail closed when a positive packing advance cannot be represented faithfully.

## Expected surfaces

- `src/QS3D.Core/Documentation/SemanticSheetAutoLayoutPlanner.cs`
- `tests/QS3D.Core.SmokeTests/SemanticSheetAutoLayoutGapPrecisionSmoke.cs`

## Excluded scope

- Semantic schedule placement, semantic sheet definition/planner behavior outside the auto-layout packing path.
- Quantity/reporting, Geometry quantity explainer, release/V25 automation, LOCAL-003, QSDB, IFC, Rebar, Family/report schedule lanes.
- General floating-point refactors unrelated to preserving configured auto-layout gaps.

## Acceptance

- A positive horizontal gap that falls below the local double ULP now fails closed instead of placing the next view with zero represented gap.
- A positive vertical gap lost while wrapping to the next row now fails closed instead of placing the next row without the requested gap.
- Ordinary finite gaps retain deterministic placement coordinates/pagination in the focused regression.
- Row fitting uses subtraction-based bounds and represented item/gap advances are checked for precision loss.

## Evidence

- Claim: `d04a5d8ffe9df33b266c6d5bf258cc3929d5fe02`
- Packing hardening: `d596b3411bcd1af94dd9935f73c7ab1f7ebf0e3a`
- Row-wrap correction after reviewing the first refactor: `9610bcc9c7f08f2caa841685b7b94b852d931141`
- Focused standalone regression: `00d6e68c9492a0c9dbcb04215bab7ecbb9c1a006`
- Regression covers lost horizontal `1 mm` gap at `1e16`, lost vertical `1 mm` row gap at `1e16`, and an ordinary finite-gap control (`A@10,10`, `B@68,10`).
- Remote source/test contents and commit diffs were re-read after publication.
- Native/.NET smoke execution was not available in this runtime; no GitHub Actions were dispatched and no runtime PASS is claimed.

## Coordination

The prior auto-layout margin-precision claim is completed and established fail-closed precision semantics for this class. The implementation remained restricted to auto-layout packing plus a standalone `ModuleInitializer` smoke and did not touch shared smoke registration or unrelated ACTIVE lanes.

## Completion condition

Satisfied: source hardening and focused regression are on `main`, evidence is recorded above, and this claim is closed `COMPLETED`.
