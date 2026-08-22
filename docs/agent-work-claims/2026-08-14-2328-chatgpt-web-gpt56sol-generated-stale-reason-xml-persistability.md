# Agent work claim — generated stale reason XML persistability

- Agent: `chatgpt-web-gpt56sol-generated-stale-reason-xml-persistability`
- Date: 2026-08-14
- Status: `COMPLETED`
- Baseline main SHA: `ae61c986850d599bba92acd4b1e190c669b5f551`
- Claim commit: `bda67a271f5622696f0d072200bb233bd21e4329`
- Implementation branch: `agent/chatgpt-web-gpt56sol/generated-stale-reason-xml-persistability-20260814`
- Source commit: `710f64b81858c6eab37ed2d2d0c7048c60fed4ff`
- Initial regression commit: `e06c07fe21b7bec4b067dc7055d2ff373cf49a87`
- Corrected regression / implementation head: `9ee9a0824f48799daefe26f1f91bc42679e22bd3`
- Integration branch: `integration/chatgpt-web-gpt56sol-generated-stale-reason-xml-persistability-20260814`
- Final integration / source landing: `ae36deb017f9693ba6f9285617fb4eca255ea8b5`
- Priority: Core P1 persistence integrity / mutation atomicity

## Reserved scope

Fixed one confirmed public generated-output stale-reason persistability defect in `ProjectElement.MarkGeneratedGeometryStale`, `MarkGeneratedCurtainFrameStale`, and `MarkGeneratedCurtainPanelStale`. These public methods ultimately write the caller-supplied reason into `Properties[QS3D.GeneratedGeometry.StaleReason]`, which is persisted as an XML attribute, but the reason previously received only blank-default/trim normalization and no XML-character validation.

Because the stale methods mutate output state/snapshot properties before writing the aggregate reason, validation is now performed at the public preflight boundary so XML-invalid input fails before any stale-state mutation.

## Changed surfaces

- `src/QS3D.Core/Domain/ProjectElement.cs` — added `NormalizeStaleReason(...)`, preserving blank-default/trim semantics and verifying XML characters through the existing `RequireXmlText(...)`; all three public stale-marking entry points preflight the reason before output-state/snapshot mutation.
- `tests/QS3D.Core.SmokeTests/GeneratedStaleReasonPersistabilitySmoke.cs` — verifies `U+0001` rejection is atomic for the full property map, Dirty and UpdatedUtc; verifies valid normalized newline/tab reason state and QSDB SaveNew→Load round-trip.
- this claim file.

## Explicit non-scope

- No changes to public `SetProperty` value persistability (completed separately), Family properties (owned by another agent), raw `Properties` dictionary semantics, generated output handle parsing/canonicalization, health diagnostics, regeneration policies, rebar/export/UI/native/CI/signing, QSDB schema/migration, or LOCAL_ONLY qualification.
- No manual GitHub Actions dispatch/rerun/cancel.

## Evidence and validation

- Baseline `ae61c986850d599bba92acd4b1e190c669b5f551`: public stale entry points could mutate state/snapshot properties and then persist an XML-invalid reason without validation, leaving canonical Save to fail later.
- Source `710f64b81858c6eab37ed2d2d0c7048c60fed4ff` performs normalized reason XML preflight before mutation.
- Initial regression `e06c07fe21b7bec4b067dc7055d2ff373cf49a87` was caught during readback using a nonexistent enum token; corrected regression head `9ee9a0824f48799daefe26f1f91bc42679e22bd3` uses the current `GlassWall` category. The invalid fixture was never integrated into `main`.
- Agent-branch compare from claim commit reports only the reserved source and focused smoke files.
- Integration candidate `ae36deb017f9693ba6f9285617fb4eca255ea8b5` was built from fresh `main` `651dd92f1a7983c6143ac350200e8430fc55d8ec`, compared cleanly as only the two reserved files, and landed on `main` via `force:false`.
- Post-landing readback confirms all three public stale entry points call `NormalizeStaleReason(...)` before generated-output mutation on exact landing SHA `ae36deb017f9693ba6f9285617fb4eca255ea8b5`.
- Automatic post-main integration dispatcher run `31819892695` was observed for exact source landing SHA; status at closeout observation was `in_progress`, conclusion not yet available.
- No executable .NET Core smoke PASS, downstream cloud-build PASS, or BricsCAD/native runtime PASS is claimed without corresponding evidence.

## Completion condition

Satisfied: claim-first reservation, isolated source + corrected focused regression, fresh-main integration, non-force landing, remote readback, automatic-CI observation, and explicit validation limits are recorded.
