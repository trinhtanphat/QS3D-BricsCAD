# Work claim — Dependency health duplicate element identity

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-dependency-duplicate-element-id`
- Registered: `2026-08-12T08:37:00+07:00`
- Baseline main SHA: `bbbca9ea8674dacef9a471aff2916eb1d044de1e`
- Priority: P1 — dependency health must fail visible when semantic graph identities are duplicated.
- Task Key: `CORE-DEPENDENCY-DUPLICATE-ELEMENT-ID`

## Confirmed defect

`DependencyHealthService.Inspect(...)` counted duplicate semantic element IDs and excluded those IDs from graph construction so traversal could not bind an ambiguous node. However, it only emitted `DEPENDENCY_TARGET_AMBIGUOUS` when another dependency pointed at the duplicate ID. A project containing two elements with the same ID and no dependency edge could therefore return zero dependency-health issues even though the graph identity was invalid.

## Implemented fix

- Added deterministic `DEPENDENCY_ELEMENT_ID_DUPLICATE` error evidence for every duplicated semantic element ID before relation-specific diagnostics.
- Duplicate IDs remain excluded from graph traversal, preserving the existing ambiguity-safety behavior.
- Existing ambiguous-target, missing/blank/noncanonical/duplicate relation, self-cycle/cycle and null-element behavior is unchanged.
- Inspection remains read-only.

## Regression coverage

`tests/QS3D.Core.SmokeTests/DependencyDuplicateElementIdentitySmoke.cs` covers:

- two semantic elements with the same ID and no dependency edge still produce exactly one duplicate-identity error for that duplicated ID;
- error severity is `HealthSeverity.Error` and the reported element identity is deterministic;
- a project with unique semantic element IDs does not produce the new issue.

## Integration evidence

- Claim registration: `dff21c59a8bbd46460a8a8fe746d5025757ebd1c`.
- Source fix: `32c90b22f6cdeac793e458835e99b024afa3bebe`.
- Focused Core smoke: `f6235b6301f93b13eed6a45e7bb7b72fdd4cbdba`.
- Source and smoke were read back from current `main` after concurrent commits.
- Comparison from smoke commit `f6235b6301f93b13eed6a45e7bb7b72fdd4cbdba` to then-current `main` `379b5e0c8819df6b53f662d121aace48c95e6ef8` was `ahead`, `ahead_by=1`, `behind_by=0`, with the smoke commit as merge base.

## Validation boundary

Committed deterministic Core smoke coverage plus source/readback/ancestry review. No GitHub Actions were dispatched, no full local .NET build PASS is claimed, and no licensed BricsCAD V25 runtime PASS is claimed.
