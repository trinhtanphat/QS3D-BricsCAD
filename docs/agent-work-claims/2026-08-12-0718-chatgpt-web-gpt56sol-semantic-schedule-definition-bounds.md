# Work claim — Semantic Schedule definition constructor bounds

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T07:18:00+07:00`
- Baseline main SHA observed: `ac3090219050c8c67220e5443447647e0ec20c59`
- Priority: P1 — deterministic Core resource-bound correctness.

## Confirmed defect

`SemanticScheduleDefinition` is a public snapshot constructor that accepts lazy `IEnumerable` inputs. It currently creates unrestricted `List<string>` snapshots for include/exclude element ids and an unrestricted `List<SemanticDocumentationColumn>` snapshot for columns. The catalog already defines hard supported capacities of 5,000 include/exclude ids and 32 columns, but those limits are applied only later by `SemanticScheduleCatalog.Normalize()`. A huge or non-terminating lazy source can therefore be enumerated/allocated without bound before the existing capacity contract is reached.

## Reserved scope

- `src/QS3D.Core/Documentation/SemanticScheduleCatalog.cs` — `SemanticScheduleDefinition` constructor snapshot materialization for include ids, exclude ids and columns only, plus minimal shared helper/constants needed to preserve the existing limits.
- Focused Core smoke regression for lazy over-bound include/exclude ids and columns.
- Isolated static preflight and planning note.

## Explicit exclusions

- Existing 128-definition catalog `Save()` bound and `Build()` Floor/Zone canonical filtering already completed in PRs #574/#581.
- Categories collection policy; there is no separate cardinality contract to invent in this lane.
- XML schema/payload, schedule rendering, include/exclude semantic validation, duplicate handling, editor/native table placement/UI.
- BricsCAD V25/V26 runtime qualification.

## Implementation plan

1. Re-fetch moving `main` after claim and confirm constructor snapshots remain unrestricted.
2. Materialize include/exclude ids with the existing 5,000-item capacity while preserving snapshot immutability; observe item 5,001 as the rejection boundary and never request item 5,002.
3. Materialize columns with the existing 32-item capacity; observe item 33 as the rejection boundary and never request item 34.
4. Preserve later `Normalize()` validation semantics, duplicate checks, null/malformed validation timing for in-capacity values, immutable public collections and all catalog serialization/build behavior.
5. Add adversarial smoke enumerables that throw a sentinel if enumeration advances past the first over-bound item.
6. Add focused static preflight and planning documentation.
7. Refresh moving `main`, verify zero reserved-source overlap, merge only a focused PR with expected-head protection, then close this claim with exact evidence.

## Validation policy

This is pure Core constructor/resource-bound behavior. GitHub Actions are manual-only and will not be dispatched. Executable smoke/preflight PASS and licensed BricsCAD runtime PASS will not be claimed without actual execution evidence.
