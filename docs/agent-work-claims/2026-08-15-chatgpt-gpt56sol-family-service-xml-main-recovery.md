# Work claim — ProjectFamilyService XML current-main recovery

- Status: `RELEASED` — implementation complete; pending authorized review/integration
- Agent: `chatgpt-gpt56sol-family-service-xml-main-recovery-20260815`
- Registered: `2026-08-15T11:28+07:00`
- Exact main baseline: `079e0e760cc0eac8704909ab042228641c703f4d`
- Latest reconciled main: `0bf036c49fa7efdc04745f8a2af57e390d2b8cd7`
- Issue: `#1491`
- Replacement PR: `#1590`
- Historical reviewed PRs: `#1422`, `#1493`
- Branch: `agent/chatgpt-gpt56sol/family-service-xml-main-recovery-20260815`
- Priority: Core P1 persistence / failure atomicity

## Recovered current-main gap

The abandoned integration-v2 lineage left two reviewed `ProjectFamilyService` XML guards absent from current `main`:

- `Value(...)` accepted XML-illegal Family property values before `SetProperty(...)` could mutate project/Family/member state;
- `Required(...)` accepted XML-illegal Family ids/names, lookup ids and property keys before service mutation paths.

The recovery restores both reviewed guards while preserving the current-main Family assignment/inheritance/ownership/freshness logic.

## Exact recovery scope

- `src/QS3D.Core/Domain/ProjectFamilyService.cs`
- `tests/QS3D.Core.SmokeTests/FamilyPropertyValuePersistabilitySmoke.cs`
- `tests/QS3D.Core.SmokeTests/ProjectFamilyServiceXmlPersistabilitySmoke.cs`
- `tests/QS3D.Core.SmokeTests/ProjectFamilyServiceXmlPersistabilityRegistration.cs`
- this claim file

## Evidence

- claim-only: `1ea43498338af5347defea8fc95f408466cb1ccb`
- implementation from reviewed blobs: `d44c7910ccfa5b0ed4610e2e28aba1e324c7755c`
- non-force reconciliation: `8d745444bad8a6c24c2cfcdef787923c2bc8a2f9`
- PR: `#1590` — ready for review; direct compare at handoff was ahead 3 / behind 0 with exactly five task files
- production source delta: `+17/-0` only (`System.Xml` + two helper XML guards)
- reviewed combined source blob: `c5d3aabb8ace45fb1f74218666ce73bd820d7760`
- reviewed property-value smoke blob: `fcbe964a7cb9af0bb37b7322be2fe52a913cfacd`
- reviewed service smoke blob: `c5280c5f17b9c4129a62c11ef3e8c798d91210d2`
- reviewed registration blob: `06ca053f696e0b3f2f318da5c9d572caee18f61a`
- exact GitHub source/diff readback: PASS
- managed build/smoke: **NOT_RUN** — `dotnet` is unavailable; no managed PASS claimed
- BricsCAD runtime: not applicable to this Core-only lane
- GitHub Actions: not manually dispatched/rerun

## Exclusions / handoff

No ProjectState, ProjectElement, serializer/schema, adapter/native, workflow/release or product-boundary changes. No direct main merge by this normal-agent session.

Reservation ownership is released. Keep #1491 open until an authorized coordinator integrates #1590 and exact-main ancestry/source readback proves both Family service guards landed.
