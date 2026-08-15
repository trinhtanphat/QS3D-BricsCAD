# Work claim — ZoneDefinition XML persistability

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-zone-xml-20260815`
- Registered: `2026-08-15T08:30:19+07:00`
- Baseline main SHA: `bbf44df6f5440566758122866289ea60973e155c`
- Issue: `#1442`
- Branch: `agent/chatgpt-gpt56sol/zone-xml-persistability-20260815`
- Priority: Core P1 persistence integrity

## Confirmed defect

`ZoneDefinition` accepts XML-illegal UTF-16 such as an unpaired surrogate because its public constructor and `Name` setter validate only blank/control-character input. `QsdbProjectStore.Save*` serializes Zone `id`/`name` to XML and rejects that accepted state later, so the public mutation boundary is not aligned with canonical persistence and an invalid `Name` assignment is not fail-fast.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectState.cs`: harden `ZoneDefinition` text validation only.
- `tests/QS3D.Core.SmokeTests/ZoneDefinitionXmlPersistabilitySmoke.cs`.
- `tests/QS3D.Core.SmokeTests/ZoneDefinitionXmlPersistabilityRegistration.cs`.
- This claim file for handoff/closeout only.

## Excluded scope

- No `FloorDefinition`, `ProjectFamily`, other `ProjectState` scalar/identity fields, elements, quantities, adapter/native, release, workflow, or documentation behavior.
- No overlap with #1401 / PR #1424.
- No GitHub Actions dispatch/re-run and no licensed BricsCAD runtime claim.
- No direct write or merge to `main`; normal-agent stop point is branch + PR unless separately authorized for integration.

## Acceptance

- XML-invalid Zone id/name is rejected at the Zone public boundary.
- Failed `Name` assignment preserves the prior Zone name exactly.
- Valid supplementary Unicode remains accepted and survives exact `QsdbProjectStore.SaveNew` → `Load` round-trip.
- Reconcile against fresh `main`, inspect final diff/readback, and report validation truthfully without inventing `LOCAL_PASS`.
