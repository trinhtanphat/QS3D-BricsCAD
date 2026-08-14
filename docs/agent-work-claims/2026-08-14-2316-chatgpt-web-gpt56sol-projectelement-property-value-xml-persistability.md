# Agent work claim — ProjectElement property value XML persistability

- Agent: `chatgpt-web-gpt56sol-projectelement-property-value-xml-persistability`
- Date: 2026-08-14
- Status: `ACTIVE`
- Baseline main SHA: `92b21ccff6600ab07c9b23d3029275aa74decda9`
- Claim commit: `daf6b9485c72ce315fcaa15d10f4e60ebbee8cef`
- Implementation branch: `agent/chatgpt-web-gpt56sol/projectelement-property-value-xml-persistability-20260814`
- Source commit: `f3501927642c2bb1b6fab0ad63335d7de1c1ca53`
- Regression commit / implementation head: `38113becd5451259d7aab91b9acbdee8f12e178b`
- Planned integration branch: `integration/chatgpt-web-gpt56sol-projectelement-property-value-xml-persistability-20260814`
- Priority: Core P1 persistence integrity

## Reserved scope

Fix one confirmed public-writer-to-QSDB persistability mismatch in `ProjectElement.SetProperty`. The method already requires a canonical non-empty property name and rejects control characters in the key, but it accepts arbitrary property value text. QSDB persists every element property value directly as an XML attribute, so XML-illegal text such as `U+0001` can enter supported element state and mutate dirty/timestamp state before canonical Save rejects it.

This lane only requires public `SetProperty` values to be XML-representable before backing mutation. Null values remain canonicalized to empty string; valid whitespace/newline/tab text remains accepted. No property business semantics, generated-geometry dependency policy, or raw persistence hydration contract is changed.

## Expected surfaces

- `src/QS3D.Core/Domain/ProjectElement.cs` — validate normalized public property value with XML-character rules before dictionary mutation/dirty propagation.
- new focused `tests/QS3D.Core.SmokeTests/ProjectElementPropertyValuePersistabilitySmoke.cs` — invalid value rejection atomicity plus valid XML text QSDB round-trip.
- this claim file for coordination/closeout evidence.

## Explicit non-scope

- No change to property-name normalization, quantity writers, relation ids, drawing fingerprint, SourceHandles/DependsOn collections, generated-output stale policy, raw `Properties` dictionary mutation semantics, QSDB loader/schema/migration, Family properties, project metadata, UI/native/rebar/export/CI/signing lanes, or LOCAL_ONLY qualification.
- No manual GitHub Actions dispatch/rerun/cancel.

## Evidence before registration

At baseline `92b21ccff6600ab07c9b23d3029275aa74decda9`, `ProjectElement.SetProperty` trims/validates the key but assigns `value ?? string.Empty` directly to `Properties`. `QsdbProjectStore.Serialize(...)` writes every property pair through `Map(...)` as XML `name`/`value` attributes and `ValidateSerializedXmlText(...)` verifies XML characters. Therefore `SetProperty("Note", "bad\u0001value")` is accepted at the public writer boundary but cannot be serialized as canonical QSDB XML.

No matching current claim/commit was found for ProjectElement property-value XML persistability.

## Implementation evidence

- `f3501927642c2bb1b6fab0ad63335d7de1c1ca53` adds `XmlConvert.VerifyXmlChars` validation for the normalized public property value before no-op detection, backing-map mutation, or dirty/timestamp propagation.
- `38113becd5451259d7aab91b9acbdee8f12e178b` adds focused smoke coverage proving `U+0001` replacement rejection is failure-atomic for value/Dirty/UpdatedUtc and that XML-valid whitespace/newline/tab text round-trips exactly through QSDB SaveNew/Load.
- Agent-branch compare from claim commit reports only the reserved source file plus the new focused smoke file.
- Executable .NET/native validation has not been run in this connector-only environment; no runtime PASS is claimed.

## Validation plan

- verify claim visibility on refreshed `main` and re-check overlap before source work;
- add the smallest pre-mutation XML-character validation while preserving null-to-empty and valid text semantics;
- add deterministic self-registering smoke proving invalid value rejection leaves existing value, Dirty, and UpdatedUtc unchanged, while valid XML text round-trips through SaveNew/Load;
- read back source/test diff, reconcile against fresh `main`, final landing with `force:false`, observe automatic CI only, and record actual evidence without manufacturing PASS.

## Completion condition

Claim-first reservation, isolated source + focused regression, fresh-main integration/readback, and truthful CI/native boundaries are recorded; then status becomes `COMPLETED`.
