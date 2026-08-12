# Agent Work Claim

- Agent: `chatgpt-gpt56-sol-qsdb-element-references`
- Slice: `QSDB element family/floor/zone referential integrity`
- Scope: `Reject current-schema QSDB elements whose non-empty familyId, floorId, or zoneId does not resolve to the corresponding persisted catalog, on both load and pre-publication serialized validation; preserve blank optional references and case-insensitive semantic ID identity.`
- Allowed paths:
  - `src/QS3D.Core/Persistence/QsdbProjectXmlSchemaValidator.cs`
  - `tests/QS3D.Core.SmokeTests/QsdbActiveContextReferentialIntegritySmoke.cs`
  - `docs/agent-work-claims/chatgpt-qsdb-element-references-2026-08-12.md`
- Shared files: `none`
- Dependencies: `none`
- Validation owner: `chatgpt-gpt56-sol-qsdb-element-references`
- Test transfer: `Extend the already-registered QSDB referential-integrity smoke with orphan family/floor/zone element references, resolved references, blank optional references, and Save publication rejection. Do not dispatch GitHub Actions.`
- Status: `ACTIVE`
