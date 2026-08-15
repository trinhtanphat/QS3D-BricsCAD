# Work claim — ProjectFamilyService XML current-main recovery

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-family-service-xml-main-recovery-20260815`
- Registered: `2026-08-15T11:28+07:00`
- Exact main baseline: `079e0e760cc0eac8704909ab042228641c703f4d`
- Issue: `#1491`
- Historical reviewed PRs: `#1422`, `#1493`
- Branch: `agent/chatgpt-gpt56sol/family-service-xml-main-recovery-20260815`
- Priority: Core P1 persistence / failure atomicity

## Confirmed current-main gap

The abandoned integration-v2 lineage left two reviewed `ProjectFamilyService` XML guards absent from current `main`:

- `Value(...)` still accepts XML-illegal Family property values before `SetProperty(...)` mutates project/Family/member state;
- `Required(...)` still accepts XML-illegal Family ids/names, lookup ids and property keys before service mutation paths.

Canonical QSDB persistence later rejects the same text. Both guards were independently reviewed in #1422 and #1493 but never landed on the current-main lineage.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectFamilyService.cs`
- focused Family property-value persistability smoke from #1422
- focused Family service XML/failure-atomicity smoke and module registration from #1493
- this claim file

## Recovery contract

- restore `System.Xml` and `XmlConvert.VerifyXmlChars(...)` guards only in existing `Required(...)` and `Value(...)` helpers;
- preserve required-text trim/length/control semantics and property-value null/length semantics;
- XML-invalid service inputs fail before project/Family/member mutation;
- valid supplementary Unicode and XML-valid whitespace/newline/tab remain supported and QSDB-round-trippable;
- preserve all assignment/inheritance/ownership/freshness/rollback behavior;
- no ProjectState, ProjectElement, serializer/schema, adapter/native, workflow/release or product-boundary changes.

No direct main merge and no manual GitHub Actions dispatch/rerun.
