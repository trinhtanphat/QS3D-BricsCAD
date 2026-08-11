# Work claim — Semantic Schedule catalog save bounded enumeration

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T00:23:00+07:00`
- Baseline main SHA observed: `67e822d06f13f4964facc2fda81913d1be8bf315`
- Priority: P1 — deterministic Core persistence/resource-bound correctness.

## Confirmed defect

`SemanticScheduleCatalog.Save()` declares `MaxSchedules = 128`, but currently executes `definitions.ToList()` before `ValidateCatalog(list)` enforces that capacity. A large or non-terminating lazy `IEnumerable<SemanticScheduleDefinition>` can therefore be enumerated/allocated without bound before the existing 128-definition guard runs.

## Reserved scope

- `src/QS3D.Core/Documentation/SemanticScheduleCatalog.cs` — `Save()` input materialization only.
- Focused Core smoke regression for lazy oversize save input.
- Focused static preflight for this bounded-enumeration contract.
- A planning note for this lane.

## Explicit exclusions

- Semantic schedule placement/native CAD tables and ownership.
- Schedule Hub/WPF/UI command flows.
- Existing XML schema/canonical metadata semantics.
- `Build()` filtering/rendering semantics.
- Floor/Zone/Element reference validation.
- BricsCAD V25 runtime qualification.
- Any currently claimed updater, opening, quantity, rebar, bulk-edit, installer or source-handle lane.

## Implementation plan

1. Re-fetch `main` after this claim and confirm the defect still exists.
2. Replace full `definitions.ToList()` with one-pass bounded materialization that accepts at most 128 definitions and throws on the 129th yielded item before persistence mutation.
3. Preserve `ValidateCatalog`, canonical serialization, empty-catalog removal and no-op payload semantics.
4. Add an adversarial Core smoke source that is effectively unbounded, fails if item 130 is requested, and proves rejection occurs after exactly 129 yields with `ProjectState.ChangeVersion` and metadata unchanged.
5. Add a static preflight rejecting the legacy pre-cap `.ToList()` path and requiring guard-before-add/validation/persistence ordering.
6. Refresh moving `main`; if the reserved source changed concurrently, stop and reconcile rather than overwrite.
7. Merge only a focused, non-overlapping batch and close this claim with exact merge evidence.

## Validation policy

This is pure Core and can be proven by deterministic smoke/static coverage. GitHub Actions remain manual-only and are not dispatched by this lane. No licensed BricsCAD V25 runtime PASS will be claimed without actual local evidence.
