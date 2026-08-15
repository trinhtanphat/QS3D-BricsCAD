# Work claim — ProjectZoneService XML current-main recovery

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-zone-service-xml-main-recovery-20260815`
- Registered: `2026-08-15T11:35+07:00`
- Exact main baseline: `0bf036c49fa7efdc04745f8a2af57e390d2b8cd7`
- Issue: `#1469`
- Historical reviewed PR: `#1470`
- Branch: `agent/chatgpt-gpt56sol/zone-service-xml-main-recovery-20260815`
- Priority: Core P1 persistence / failure atomicity

## Confirmed current-main gap

`ProjectZoneService.Required(...)` still accepts XML-illegal UTF-16 after required/trim/length/control validation. The public ZoneDefinition boundary is already XML-safe, so `Update(...)` can call `project.Touch()` before the later Zone setter rejects such text.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectZoneService.cs`
- focused Zone service XML persistability smoke from #1470
- focused module registration from #1470
- this claim file

## Recovery contract

- restore only the reviewed `System.Xml` import and `XmlConvert.VerifyXmlChars(...)` guard in `Required(...)`;
- preserve existing service/business semantics;
- XML-invalid Create/Update/lookup text must fail before mutation;
- valid supplementary Unicode must survive service operations and QSDB round-trip;
- no ProjectState, Floor/Family service, ProjectElement, serializer/schema, adapter/native, workflow/release or product-boundary changes.

No direct main merge and no manual GitHub Actions dispatch/rerun.
