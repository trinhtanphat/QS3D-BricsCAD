# Work claim — Semantic Schedule definition constructor bounds

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T07:18:00+07:00`
- Completed: `2026-08-12T07:23:00+07:00`
- Baseline main SHA observed: `ac3090219050c8c67220e5443447647e0ec20c59`
- Claim commit: `4ab318580aaf47925d2df68f027f4919b70cf0e8`
- PR: `#612`
- Squash merge on `main`: `9e1057b1bc9a8d2786ecc6bdeb7d3e210d4aa4dd`
- Priority: P1 — deterministic Core resource-bound correctness.

## Defect closed

`SemanticScheduleDefinition` is a public snapshot constructor that accepts lazy `IEnumerable` inputs. It previously created unrestricted snapshots for include/exclude element ids and columns even though the catalog already supports at most 5,000 include/exclude ids and 32 columns. Huge or non-terminating lazy sources could therefore be consumed without bound before downstream `Normalize()` reached the existing capacities.

## Implemented

- Include element ids now use bounded one-pass snapshot materialization with the existing 5,000-id catalog capacity.
- Exclude element ids use the same existing 5,000-id capacity.
- Columns use the existing 32-column capacity.
- Item 5,001 / column 33 triggers the existing capacity message immediately; item 5,002 / column 34 is never requested after oversize is known.
- `SemanticScheduleCatalog.MaxIds` / `MaxColumns` are shared internally with the definition constructor so the constructor and downstream catalog validation use one source of truth.
- Accepted collections remain defensive `AsReadOnly()` snapshots.
- Categories are unchanged; this lane does not invent a categories cardinality rule.
- Downstream duplicate/null/malformed/canonical semantic validation, XML schema, Save/Build behavior and native table placement remain unchanged.
- Added adversarial Core smoke coverage for include ids, exclude ids and columns, plus defensive snapshot non-regression, isolated registration, static preflight and planning documentation.

## Reserved scope

- `src/QS3D.Core/Documentation/SemanticScheduleCatalog.cs` — `SemanticScheduleDefinition` constructor snapshot materialization for include ids, exclude ids and columns only, plus minimal shared helper/constants needed to preserve the existing limits.
- Focused Core smoke regression for lazy over-bound include/exclude ids and columns.
- Isolated static preflight and planning note.

## Explicit exclusions honored

- Existing 128-definition catalog `Save()` bound and `Build()` Floor/Zone canonical filtering completed separately in PRs #574/#581.
- Categories collection policy.
- XML schema/payload, schedule rendering, include/exclude semantic validation, duplicate handling, editor/native table placement/UI.
- BricsCAD V25/V26 runtime qualification.

## Validation evidence

- Post-claim `main` source was re-read at `4ab318580aaf47925d2df68f027f4919b70cf0e8` and confirmed unrestricted constructor snapshots remained before implementation.
- Branch changed exactly five files; production source diff was limited to constructor bounded snapshot plumbing and shared visibility of the existing two capacity constants (+28/-5).
- PR #612 full diff was reviewed before merge.
- Moving-main comparison showed 18 concurrent commits after the claim point and another six commits after PR base, with zero overlap in `SemanticScheduleCatalog.cs` or this lane's four new files.
- Squash merge used expected head `8cf74350f5d13fd1d0d2e03d5451810dfc10f513` and succeeded as `9e1057b1bc9a8d2786ecc6bdeb7d3e210d4aa4dd`.
- GitHub Actions were not dispatched because repository policy is manual-only.
- Executable smoke/preflight PASS and licensed BricsCAD V25/V26 runtime PASS are not claimed from this connector-only environment.
