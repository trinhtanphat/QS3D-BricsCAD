# Agent work claim — ProjectElement property value XML persistability

- Agent: `chatgpt-web-gpt56sol-projectelement-property-value-xml-persistability`
- Date: 2026-08-14
- Status: `COMPLETED`
- Baseline main SHA: `92b21ccff6600ab07c9b23d3029275aa74decda9`
- Claim commit: `daf6b9485c72ce315fcaa15d10f4e60ebbee8cef`
- Implementation branch: `agent/chatgpt-web-gpt56sol/projectelement-property-value-xml-persistability-20260814`
- Source commit: `f3501927642c2bb1b6fab0ad63335d7de1c1ca53`
- Regression commit / implementation head: `38113becd5451259d7aab91b9acbdee8f12e178b`
- Integration branch: `integration/chatgpt-web-gpt56sol-projectelement-property-value-xml-persistability-20260814`
- Initial integration candidate: `b41feddead5a75d57860657c3b2f64c8577af17d`
- Reconciliation candidate: `9e665be97ec8c2dfc74a89abc8c4dfad318327d6`
- Final reconciled integration / source landing: `86c7ac72ad976807a5081977453940d10311aa8b`
- Priority: Core P1 persistence integrity

## Reserved scope

Fixed one confirmed public-writer-to-QSDB persistability mismatch in `ProjectElement.SetProperty`. The method already required a canonical non-empty property name and rejected control characters in the key, but accepted arbitrary property value text. QSDB persists every element property value directly as an XML attribute, so XML-illegal text such as `U+0001` could enter supported element state and mutate dirty/timestamp state before canonical Save rejected it.

This lane only requires public `SetProperty` values to be XML-representable before backing mutation. Null values remain canonicalized to empty string; valid whitespace/newline/tab text remains accepted. No property business semantics, generated-geometry dependency policy, or raw persistence hydration contract is changed.

## Changed surfaces

- `src/QS3D.Core/Domain/ProjectElement.cs` — normalized public property values now pass `XmlConvert.VerifyXmlChars(...)` before no-op detection, backing mutation, generated-output invalidation, dirty propagation, or timestamp updates.
- `tests/QS3D.Core.SmokeTests/ProjectElementPropertyValuePersistabilitySmoke.cs` — focused `U+0001` rejection atomicity and XML-valid whitespace/newline/tab SaveNew→Load round-trip coverage.
- this claim file.

## Explicit non-scope

- No change to property-name normalization, quantity writers, relation ids, drawing fingerprint, SourceHandles/DependsOn collections, generated-output stale policy, raw `Properties` dictionary mutation semantics, QSDB loader/schema/migration, Family properties, project metadata, UI/native/rebar/export/CI/signing lanes, or LOCAL_ONLY qualification.
- Family property-value persistability is separately owned by the claim registered after this lane and was not modified here.
- No manual GitHub Actions dispatch/rerun/cancel.

## Evidence and reconciliation

- Baseline `92b21ccff6600ab07c9b23d3029275aa74decda9`: `SetProperty("Note", "bad\u0001value")` could store XML-illegal text although QSDB `Map(...)` persisted property values as XML attributes and serialized XML validation rejected the same text.
- `f3501927642c2bb1b6fab0ad63335d7de1c1ca53` adds the pre-mutation XML-character guard; `38113becd5451259d7aab91b9acbdee8f12e178b` adds the focused smoke.
- Agent-branch compare reports only the reserved source file and new smoke.
- Concurrent main changes after claim included release-signing work, QSDB family-category reference validation, V26 UI claim state, CI-package claim state, and later a separate Family property-value persistability claim; none modified `ProjectElement.cs` or this smoke.
- Freeze-gate races were reconciled without force push. Final candidate `86c7ac72ad976807a5081977453940d10311aa8b` used current main `6fce7f627104233f4860d5c734485abe7b363031` as first parent and preserved the lane ancestry as an additional parent.
- Final compare against the current integration base reported only `src/QS3D.Core/Domain/ProjectElement.cs` and `tests/QS3D.Core.SmokeTests/ProjectElementPropertyValuePersistabilitySmoke.cs`.
- Post-landing readback on `86c7ac72ad976807a5081977453940d10311aa8b` confirms the expected property-value precondition is present on remote `main`.

## Validation boundary

- Automatic post-main integration dispatcher run `31819347802` was observed for exact source landing SHA `86c7ac72ad976807a5081977453940d10311aa8b`; status at closeout observation was `in_progress` with no conclusion yet.
- No GitHub Actions workflow was manually dispatched/rerun/cancelled.
- No executable .NET Core smoke PASS, cloud build PASS, or BricsCAD/native runtime PASS is claimed without corresponding evidence.

## Completion condition

Satisfied: claim-first reservation, isolated source + focused regression, repeated fresh-main reconciliation, non-force final landing, remote source readback, automatic-CI observation, and explicit validation limits are recorded.
