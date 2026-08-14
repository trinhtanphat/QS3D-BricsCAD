# Agent work claim — generated stale reason XML persistability

- Agent: `chatgpt-web-gpt56sol-generated-stale-reason-xml-persistability`
- Date: 2026-08-14
- Status: `ACTIVE`
- Baseline main SHA: `ae61c986850d599bba92acd4b1e190c669b5f551`
- Claim commit: `bda67a271f5622696f0d072200bb233bd21e4329`
- Implementation branch: `agent/chatgpt-web-gpt56sol/generated-stale-reason-xml-persistability-20260814`
- Source commit: `710f64b81858c6eab37ed2d2d0c7048c60fed4ff`
- Initial regression commit: `e06c07fe21b7bec4b067dc7055d2ff373cf49a87`
- Corrected regression / implementation head: `9ee9a0824f48799daefe26f1f91bc42679e22bd3`
- Planned integration branch: `integration/chatgpt-web-gpt56sol-generated-stale-reason-xml-persistability-20260814`
- Priority: Core P1 persistence integrity / mutation atomicity

## Reserved scope

Fix one confirmed public generated-output stale-reason persistability defect in `ProjectElement.MarkGeneratedGeometryStale`, `MarkGeneratedCurtainFrameStale`, and `MarkGeneratedCurtainPanelStale`. These public methods ultimately write the caller-supplied reason directly into `Properties[QS3D.GeneratedGeometry.StaleReason]`, which is persisted as an XML attribute, but the reason currently receives only blank-default/trim normalization and no XML-character validation.

A value such as `"bad\u0001reason"` can therefore be accepted into canonical element state and later rejected by QSDB serialization. Because the current stale methods mutate output state/snapshot properties before calling `SetAggregateStaleReason`, validation must occur at the public preflight boundary to remain failure-atomic.

This lane only validates the normalized reason before any stale-state mutation and then reuses the existing stale/output logic. It does not change generated handle signatures, output-presence detection, stale state tokens, default reason text, dirty flags, or clearing semantics.

## Expected surfaces

- `src/QS3D.Core/Domain/ProjectElement.cs` — normalize + verify XML characters for generated stale reasons before any stale-state/snapshot mutation in the three public stale-marking entry points.
- new focused `tests/QS3D.Core.SmokeTests/GeneratedStaleReasonPersistabilitySmoke.cs` — invalid reason failure atomicity plus valid reason QSDB round-trip.
- this claim file.

## Explicit non-scope

- No changes to public `SetProperty` value persistability (completed separately), Family properties (owned by another active claim), raw `Properties` dictionary semantics, generated output handle parsing/canonicalization, health diagnostics, regeneration policies, rebar/export/UI/native/CI/signing, QSDB schema/migration, or LOCAL_ONLY qualification.
- No manual GitHub Actions dispatch/rerun/cancel.

## Evidence before registration

At baseline `ae61c986850d599bba92acd4b1e190c669b5f551`, each public generated-stale entry point first calls `MarkGeneratedOutputStale(...)` / `MarkGeneratedCurtainPanelOutputStale(...)`, which can mutate persisted state and stale-snapshot properties, and only afterwards calls `SetAggregateStaleReason(reason)`. `SetAggregateStaleReason` trims/defaults the reason and writes it directly to `Properties[GeneratedGeometryStaleReasonKey]` with no XML validation. Thus an XML-illegal reason can both create non-persistable state and make a future validation fix non-atomic if applied only inside the final setter.

No matching current claim/commit was found for generated stale-reason XML persistability.

## Implementation evidence

- `710f64b81858c6eab37ed2d2d0c7048c60fed4ff` introduces `NormalizeStaleReason(...)`, preserves the existing blank-default/trim semantics, verifies XML characters through the existing `RequireXmlText(...)` helper, and performs this preflight before output-state/snapshot mutation in all three public stale entry points.
- `e06c07fe21b7bec4b067dc7055d2ff373cf49a87` added the initial focused smoke; readback caught an invalid fixture enum (`CurtainWall`) before integration.
- `9ee9a0824f48799daefe26f1f91bc42679e22bd3` corrects that fixture to the current `GlassWall` enum. The smoke proves invalid `U+0001` reason leaves the complete property map, Dirty and UpdatedUtc unchanged, then verifies a valid normalized reason and XML-valid newline/tab text round-trip through QSDB SaveNew/Load.
- Agent-branch compare from the claim commit reports only the reserved source file and the new focused smoke.
- No executable .NET/native PASS is claimed in this connector-only environment.

## Validation plan

- verify claim visibility on refreshed `main` and re-check overlap before source work;
- add a small stale-reason normalization/XML helper and invoke it before stale output mutation in all three public entry points;
- add deterministic smoke proving invalid `U+0001` reason leaves properties/Dirty/UpdatedUtc unchanged and valid normalized reason persists through SaveNew/Load;
- read back source/test diff, reconcile fresh `main`, final landing with `force:false`, observe automatic CI only, and record only validation actually observed.

## Completion condition

Claim-first reservation, isolated source + focused regression, fresh-main integration/readback, and truthful CI/native boundaries are recorded; then status becomes `COMPLETED`.
