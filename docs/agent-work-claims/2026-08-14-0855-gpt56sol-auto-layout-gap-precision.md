# Work claim — Semantic sheet auto-layout gap precision

- Status: `ACTIVE`
- Agent: `gpt56sol-auto-layout-gap-precision-20260814-0855`
- Registered: `2026-08-14T08:55:00+07:00`
- Baseline main SHA: `497b1936792fd0194494896128628fc4de08bf15`
- Priority: P1 documentation/layout correctness hardening; positive auto-layout gaps can currently be lost at large finite coordinates even though sibling margin/schedule placement paths fail closed on the same floating-point precision loss.

## Reserved scope

Harden `SemanticSheetAutoLayoutPlanner.PageState` packing arithmetic so configured positive horizontal/vertical gaps are never silently rounded away. Preserve ordinary packing/pagination behavior and fail closed when a positive packing advance cannot be represented faithfully.

## Expected surfaces

- `src/QS3D.Core/Documentation/SemanticSheetAutoLayoutPlanner.cs`
- a focused standalone smoke under `tests/QS3D.Core.SmokeTests/` using `ModuleInitializer`; do not touch shared smoke registration.

## Excluded scope

- Semantic schedule placement, semantic sheet definition/planner behavior outside the auto-layout packing path.
- Quantity/reporting, Geometry quantity explainer, release/V25 automation, LOCAL-003, QSDB, IFC, Rebar, Family/report schedule lanes.
- General floating-point refactors unrelated to preserving configured auto-layout gaps.

## Acceptance

- A positive horizontal gap that falls below the local double ULP fails closed instead of placing the next view with zero represented gap.
- A positive vertical gap lost while wrapping to the next row fails closed instead of placing the next row without the requested gap.
- Ordinary finite gaps keep existing deterministic placement coordinates/pagination.
- Source remains overflow-safe at paper numeric limits.

## Coordination

The prior auto-layout margin-precision claim is completed and establishes fail-closed precision semantics for this class. Current visible ACTIVE claims cover Geometry-backed quantity explanation, issue 1099 update gates, LOCAL-003 and unrelated product lanes; this claim reserves only auto-layout packing-gap arithmetic. Recheck current main and ACTIVE/BLOCKED claims before source/test writes.

## Completion condition

Minimal source hardening and focused regression are on `main`, remote lineage is verified, then this claim is updated to `COMPLETED` with exact commit evidence. Native/.NET smoke execution is reported only if actually available; no GitHub Actions are dispatched for this lane.
