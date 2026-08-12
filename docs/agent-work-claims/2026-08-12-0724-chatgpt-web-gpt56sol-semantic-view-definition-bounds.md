# Work claim — Semantic View definition filter bounds

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T07:24:00+07:00`
- Completed: `2026-08-12T07:28:00+07:00`
- Baseline main SHA observed: `a7958ce8a078e6aed627f8b825b9122d2dccb394`
- Claim commit: `148d155e397261994e7034a6451dc4d44df4f099`
- PR: `#615`
- Squash merge on `main`: `94125f449fe038ffb0e6c13acd2d69d301f5e9dc`
- Priority: P1 — deterministic Core resource-bound correctness.

## Defect closed

`SemanticViewDefinition` is a public defensive-snapshot constructor that accepts lazy include/exclude element-id enumerables. It previously materialized both without bounds, while `SemanticViewPlanner` already supported at most 100,000 include ids and 100,000 exclude ids. Huge or non-terminating sources could therefore be consumed without bound before planner validation reached the existing capacity.

## Implemented

- Include and exclude constructor snapshots now use one-pass bounded enumeration with the existing `MaxFilterIds = 100000` contract.
- The 100,001st item triggers the existing capacity message; item 100,002 is never requested after oversize is known.
- `MaxFilterIds` is shared internally between definition construction and downstream `NormalizeIds()` so the capacity cannot drift.
- Accepted include/exclude collections remain defensive read-only snapshots.
- Categories remain unchanged because no separate category cardinality policy exists.
- Downstream id-required/length validation, duplicate detection, include/exclude overlap validation, Floor/Zone reference/filter behavior, BuildCatalog capacity and deterministic ordering remain unchanged.
- Added adversarial include/exclude smoke coverage, defensive snapshot non-regression, isolated registration, static preflight and planning documentation.

## Validation evidence

- Post-claim source at `148d155e397261994e7034a6451dc4d44df4f099` confirmed unrestricted include/exclude snapshots remained before implementation.
- Branch changed exactly five files; production source diff was +19/-3 in `SemanticViewPlanner.cs`.
- PR #615 full diff was reviewed before merge.
- Moving-main comparison found 19 concurrent commits after the claim point with zero overlap in the reserved source/lane files.
- The first expected-head merge attempt was safely rejected because the base changed. Four intervening commits were rechecked and touched only unrelated local-agent/health/snapshot surfaces; the same PR head was then squash-merged successfully as `94125f449fe038ffb0e6c13acd2d69d301f5e9dc`.
- GitHub Actions were not dispatched because repository policy is manual-only.
- Executable smoke/preflight PASS and licensed BricsCAD V25/V26 runtime PASS are not claimed from this connector-only environment.

## Explicit exclusions honored

- Categories collection policy.
- `BuildCatalog()` 10,000-view capacity, completed null-reference handling, Floor/Zone canonical filtering, element lookup/ordering semantics.
- Semantic Documentation persistence store/editor, Sheet planner, native CAD view/sheet materialization, WPF/UI.
- BricsCAD V25/V26 runtime qualification.
