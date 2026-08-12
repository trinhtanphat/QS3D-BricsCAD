# Work claim — template profile XML text preflight

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-template-profile-xml-text-preflight-20260812-0955`
- Registered: `2026-08-12T09:55:00+07:00`
- Baseline main SHA: `de81a936d5125654bd44176dead1c0a658781234`
- Priority: evidence-driven remote-safe template persistence integrity

## Confirmed defect

`TemplateProfileStore.Save(...)` calls `Validate(profile)` before resolving/creating the destination path, but current validation does not verify XML character legality for strings that `Serialize(...)` writes directly into `XAttribute` values. An in-memory template can therefore contain XML-invalid control characters or malformed surrogate sequences, pass preflight, and fail only during XML construction/write after the destination directory/temp-file workflow has begun.

## Reserved scope

- Validate XML character legality for all template strings serialized into XML attributes during the existing profile preflight.
- Include profile identity/name, family identity/name/property keys and values, quantity-rule string fields, layer-mapping strings, and visible BQ column names.
- Preserve existing null family-property value → empty-string serialization semantics.
- Fail closed with `InvalidDataException`; do not sanitize or rewrite semantic content.
- Preserve valid supplementary Unicode and all existing ordering/schema/apply/backup behavior.

## Expected surfaces

- `src/QS3D.Core/Templates/TemplateProfileStore.cs`
- one focused `QS3D.Core.SmokeTests` regression file with isolated `ModuleInitializer`
- this claim file

## Excluded scope

- Template XML schema shape/order, category-token policy, family/rule semantic validation, import freshness, UI/native runtime, release/signing.
- No GitHub Actions or LOCAL_ONLY qualification.

## Validation plan

- XML-invalid template id fails before destination directory creation.
- XML-invalid family property value fails before destination directory creation.
- malformed lone surrogate fails closed before filesystem mutation.
- valid supplementary Unicode property text round-trips exactly.
- null family property values retain the existing empty-string persistence contract.
- exact branch diff and moving-main target blob are rechecked before integration.

## Completion condition

Focused source/regression are merged to current `main`, remote source/test are re-read, and this claim is closed `COMPLETED` with exact integration evidence.
