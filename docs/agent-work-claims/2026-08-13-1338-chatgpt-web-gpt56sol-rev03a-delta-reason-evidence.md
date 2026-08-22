# Work claim — REV-03A conservative Measurement Snapshot delta reason evidence

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-rev03a-delta-reason-20260813-1338`
- Registered: `2026-08-13T13:38:34+07:00`
- Baseline main SHA: `1690e9eb3606ff2cf565accf8652dc5272d34efc`
- Priority: `REV-03 / P1` — classify only deterministic evidence already present in canonical MeasurementTrace data above completed REV-02A

## Confirmed gap

Current `src/QS3D.Core/Measurement` contains only `MeasurementTrace.cs`, `MeasurementSnapshot.cs`, and `MeasurementSnapshotDelta.cs`. REV-02A classifies line state and preserves previous/current canonical traces, but there is no contract that explains which observable trace dimensions changed. Current history checks found no `REV-03` or `delta reason` implementation/claim before registration.

The trace contract can deterministically expose changes in top-level rule identity/version, adjustment-rule provenance, input facts, adjustments, rounding policy, warnings and assumptions. It does not carry BOQ/classification mapping identity and does not prove that an input-fact change was caused specifically by geometry versus another semantic/property source. This lane therefore remains conservative and does not invent those causes.

## Reserved scope

Add one pure-Core read-only classifier over an existing `MeasurementSnapshotDeltaLine`.

The classifier will:

- return deterministic reason evidence for `Added`, `Removed`, and `Unchanged` lines;
- for `Changed` lines, classify only directly observable canonical trace differences: rule identity/version provenance, input facts, adjustments, rounding policy, and warning/assumption annotations;
- recognize adjustment rule provenance changes as rule provenance evidence without interpreting the adjustment business rule itself;
- emit `Unresolved` when a canonical trace is changed but no supported observable evidence dimension explains that change (for example only gross/net values differ with otherwise identical trace evidence);
- allow more than one evidence reason when multiple trace dimensions changed, in stable enum order;
- never label an input-fact change as geometry/property-driven unless future canonical provenance explicitly supports that distinction;
- never infer mapping/BOQ cause because mapping identity is not present in current MeasurementTrace.

## Expected surfaces

- new `src/QS3D.Core/Measurement/MeasurementSnapshotDeltaReason.cs`
- new `tests/QS3D.Core.SmokeTests/MeasurementSnapshotDeltaReasonSmoke.cs`
- new `tests/QS3D.Core.SmokeTests/MeasurementSnapshotDeltaReasonRegistration.cs`
- this claim file

## Excluded scope

- No edits to `MeasurementTrace.cs`, `MeasurementSnapshot.cs`, or `MeasurementSnapshotDelta.cs`.
- No edits to MAP-01A category/work-item mapping; its current `ACTIVE` claim remains fully owned by that agent.
- No attempt to infer classification/BOQ mapping changes until canonical mapping identity exists in a later, separately claimed integration.
- No edits to existing `RevisionService`, `RevisionSnapshot`, `QuantityRevisionReport`, persistence/schema, Quantity Rules, report/UI/XLSX, rates/cost, PERF harness, geometry, regenerators, or BricsCAD adapters.
- No second quantity/delta engine: this contract consumes REV-02A delta lines and canonical traces only.
- No GitHub Actions or BricsCAD native PASS claim.

## Validation plan

- Publish this claim alone, refresh `main`, verify it is on current lineage, and compare baseline-to-current for any new REV/Measurement overlap before source work.
- Re-read the five required governance/workstream/boundary/research docs on post-claim current `main` before substantive source change.
- Focused smoke source will cover Added/Removed/Unchanged reasons, rule-version-only change, adjustment-rule provenance change, input-fact change, adjustment change, rounding/annotation change, multiple simultaneous reasons in deterministic order, and unresolved numeric-only change.
- Re-fetch exact implementation files from pushed `main` and record remote blob identities.
- Executable managed smoke remains `NOT_RUN` unless a real checkout/.NET execution path becomes available; no native claim is applicable to this pure-Core lane.

## Coordination

- `MAP-01A category measurement/work-item mapping contract` is currently `ACTIVE` and owns the new category/classification/work-item mapping foundation. This REV-03A lane does not touch Mapping source/tests or infer mapping delta reasons.
- Curtain P11/native runtime work is concurrent and confined to host/scripts/docs surfaces; this lane does not touch them.
- REV-01A and REV-02A are completed and consumed as-is.
- Current-main recent commit scan plus targeted `REV-03` / `delta reason` history checks found no conflicting owner or implementation before registration. Connector claim-directory truncation prevents claiming an exhaustive local `rg`; exact current source tree, full neighboring claim read, recent history, and baseline-to-head diffs are used instead.

## Completion condition

A pushed pure-Core conservative delta-reason evidence classifier plus focused auto-registered smoke is present on current `main`, no existing measurement/delta/mapping engine is modified, remote implementation is re-fetched and reviewed, and this claim is updated to `COMPLETED` with exact implementation SHA and validation actually executed.
