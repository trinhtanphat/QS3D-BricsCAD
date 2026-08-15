# Work claim — BCF exchange XML representability current-main recovery

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-bcf-exchange-xml-main-recovery-20260815`
- Registered: `2026-08-15T10:04+07:00`
- Exact main baseline: `49135b378a479fa6c4da78d2d8713ad65b5bba61`
- Issue: `#1506`
- Superseded integration-v2 PR: `#1510`
- Branch: `agent/chatgpt-gpt56sol/bcf-exchange-xml-main-recovery-20260815`
- Priority: Core P1 interoperability / fail-fast export integrity

## Confirmed current-main defect

Current `BcfIssueExchange.cs` allows XML-illegal UTF-16 through BCF free text and BCF token fields (`status`, `type`, `qs3dElementId`) until later XML serialization. This violates the model's construction-time validity boundary.

## Reserved recovery surfaces

- `src/QS3D.Core/Export/BcfIssueExchange.cs`
- `tests/QS3D.Core.SmokeTests/BcfIssueExchangeXmlRepresentabilitySmoke.cs`
- `tests/QS3D.Core.SmokeTests/BcfIssueExchangeXmlRepresentabilityRegistration.cs`
- this claim file

## Recovery constraints

- BCF-local `XmlConvert.VerifyXmlChars(...)` validation only; do not change the global IFC token contract;
- preserve all existing canonical GUID, IFC GUID, whitespace/control, collection-bound, camera and deterministic-ordering semantics;
- preserve valid supplementary Unicode through exact serializer round-trip;
- no `BcfIssueExchangeSerializer.cs`, `BcfZipPackage.cs`, adapter/native, workflow/release, schema or product-boundary changes;
- #1513/#1512 remains a separate serializer timestamp-canonicality lane with no source overlap;
- no direct main merge and no manual GitHub Actions dispatch/rerun.

## Prior reviewed evidence

- v2 source: `1fd36d180bef77a3a71bbfbd05884e33a107014c`
- v2 smoke: `44ce0a7d18feb18fea3c85c901069b881cf3f10f`
- v2 registration: `88ba6028d65a8f2593c9bd41cdcd17efe7b6f650`

Implementation begins only after this claim is published.
