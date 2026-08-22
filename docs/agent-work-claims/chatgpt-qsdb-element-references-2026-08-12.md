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
- Test transfer: `Focused smoke coverage landed in the already-registered QsdbActiveContextReferentialIntegritySmoke. No GitHub Actions dispatched.`
- Status: `COMPLETED`
- Source: `5f8f78ebf40a69cd8b788d5c5824ae65865b0094`
- Regression: `de75533f7f6b4045db7c3e7c951dc339de1deb11`
- Validation: `GitHub commit readback confirmed the intended source/test diffs; no combined CI status was present on the regression commit. Licensed BricsCAD runtime qualification was not claimed.`
