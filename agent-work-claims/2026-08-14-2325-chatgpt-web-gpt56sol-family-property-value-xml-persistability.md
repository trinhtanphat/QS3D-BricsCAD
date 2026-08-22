# Agent work claim — Family property value XML persistability

- Agent: `chatgpt-web-gpt56sol-family-property-value-xml-persistability`
- Date: `2026-08-14`
- Status: `ACTIVE`
- Baseline main SHA: `ef20c78812740d604549f5b2a13bcc2e7fc1d777`
- Implementation branch: `agent/chatgpt-web-gpt56sol/family-property-value-xml-persistability-20260814`
- Planned integration branch: `integration/chatgpt-web-gpt56sol-family-property-value-xml-persistability-20260814`
- Priority: Core P1 supported-writer persistence integrity

## Confirmed defect

`ProjectFamilyService.SetProperty(...)` is a supported business API and validates Family property values only by maximum length. It accepts XML-illegal text such as `U+0001`, then calls `project.Touch()`, writes the Family property, and propagates to members before QSDB serialization rejects that text. Thus the supported writer can mutate revision/timestamps and semantic state into a form that canonical QSDB cannot persist.

The same private `Value(...)` helper is used by `SnapshotProperties(...)`, so malformed Family property values introduced through legacy/raw state also pass service preflight unless they exceed the length limit.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectFamilyService.cs` — require `Value(...)` text to be XML-representable before any service mutation while preserving null-to-empty, length limit, whitespace/newline/tab and existing key semantics.
- new self-registering `tests/QS3D.Core.SmokeTests/ProjectFamilyPropertyValuePersistabilitySmoke.cs` — invalid-value rejection atomicity plus valid XML text propagation/QSDB round-trip.
- this claim for coordination/close-out.

## Explicit exclusions / concurrency protection

- No changes to `ProjectElement.cs`; the ACTIVE ProjectElement property-value lane owns that writer.
- No redesign of public raw `ProjectFamily.Properties` dictionary or persistence hydration semantics.
- No changes to Family property keys, freshness/geometry policy, audit UI, assignment category semantics, QSDB schema/validator/migrator, metadata, quantity rules, rebar/export, UI/native, CI/package, release/signing or LOCAL_ONLY work.
- No `SmokeTestRegistration.cs` change; the regression uses the repository's current ModuleInitializer self-registration pattern.
- No manual GitHub Actions dispatch/rerun/cancel and no force-push.

## Validation plan

- set a valid Family property through `ProjectFamilyService.SetProperty(...)`, then attempt an XML-illegal replacement and prove Family value, project `ChangeVersion`, project `UpdatedUtc`, and member state remain unchanged;
- prove XML-valid whitespace/newline/tab property text remains accepted, propagates to an inherited member, and round-trips through QSDB;
- prove pre-existing malformed raw Family property value is rejected by a service path using `SnapshotProperties(...)` before a duplicate/assignment mutation is published;
- review exact agent diff, reconcile current `main`, perform one integration landing with `force:false`, read back source/test, and report automatic CI only as actually observed.

## Completion condition

Claim-first reservation, fail-before-mutation service validation, focused self-registering regression, reconciled one-time source landing on current `main`, remote readback/ancestry, and truthful CI/runtime evidence are recorded; then status becomes `COMPLETED`.
