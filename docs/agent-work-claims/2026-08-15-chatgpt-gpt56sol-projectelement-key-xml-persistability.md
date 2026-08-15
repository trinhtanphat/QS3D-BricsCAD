# Work claim — ProjectElement property/quantity key XML persistability

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-projectelement-key-xml-20260815`
- Registered: `2026-08-15T10:04+07:00`
- Exact main baseline: `6b686a32934ef9fd750f3ff5ade6508cc14259c9`
- Issue: `#1572`
- Branch: `agent/chatgpt-gpt56sol/projectelement-key-xml-persistability-20260815`
- Priority: Core P1 persistence / public mutation-boundary integrity

## Confirmed defect

`ProjectElement.SetProperty(...)` and `SetQuantity(...)` trim property/quantity keys and reject control characters, but do not preflight XML character representability. Canonical QSDB persistence writes those keys into XML `name` attributes and rejects XML-invalid text only later. A lone UTF-16 surrogate can therefore be accepted by either public setter, mutate dictionary/dirty/timestamp state, and leave a ProjectElement that canonical persistence cannot represent.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectElement.cs`
- one focused Core smoke for key XML persistability
- one focused module registration
- this claim file

## Required contract

- after existing trim/control checks, route normalized property/quantity keys through the existing `RequireXmlText(...)` helper;
- reject lone high and lone low surrogate keys before dictionary mutation, dirty/stale changes or `UpdatedUtc` change;
- preserve property value XML safety, quantity finite/non-negative safety, key trimming, no-op semantics and generated-output invalidation policy;
- valid supplementary-Unicode property/quantity keys must survive exact QSDB SaveNew/Load;
- do not redesign raw dictionaries and do not restrict `RemoveProperty(...)`, which remains available for repair of invalid raw state.

## Exclusions

No ProjectState/Family/Floor/Zone service, serializer/schema, adapter/native, workflow/release or product-boundary changes. No direct main merge and no manual GitHub Actions dispatch/rerun.
