# Work claim — MTR-04 adjustment rule provenance association

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-mtr04-adjustment-rule-association-20260813`
- Registered: `2026-08-13T19:07:00+07:00`
- Baseline main SHA: `9f7f5392066f5af3cb119b9a6842098180b271b8`
- Priority: P0 explainable-measurement/revision evidence integrity. `MeasurementSnapshotDeltaReasonClassifier.AdjustmentRuleProvenanceChanged()` currently reduces adjustment rule provenance to a sorted multiset of `RuleId/RuleVersion` tokens. If two distinct adjustment evidence rows keep the same global rule-token multiset but exchange which rule is attached to which source/reason row, the classifier reports only `AdjustmentsChanged` and misses `RuleProvenanceChanged`, even though per-adjustment provenance materially changed.

## Reserved scope

- `src/QS3D.Core/Measurement/MeasurementSnapshotDeltaReason.cs`
- `tests/QS3D.Core.SmokeTests/MeasurementSnapshotDeltaReasonSmoke.cs`
- this claim file for closeout

## Intended change

Preserve existing global rule-token detection, and additionally detect rule-association changes when the canonical non-rule adjustment evidence is otherwise the same. Do not infer provenance changes when the adjustment evidence itself cannot be aligned safely. Keep reason ordering, numeric-only `Unresolved`, top-level rule classification, snapshot delta arithmetic, and MeasurementTrace canonical bytes unchanged.

## Excluded scope

- no changes to `MeasurementTrace`, `MeasurementSnapshot`, or `MeasurementSnapshotDelta` arithmetic/schema;
- no new causal inference from geometry/properties/mapping;
- no Cost/CST-04 work, signed-zero lanes, report/UI/export, BricsCAD native adapters, sibling Platform migration, GitHub Actions or native qualification.

## Validation plan

- refresh `main` after claim publication and recheck recent MeasurementSnapshot/MeasurementTrace reason claims;
- add a focused regression where two distinct adjustments preserve the same rule-token multiset but swap rule assignments, expecting both `RuleProvenanceChanged` and `AdjustmentsChanged`;
- preserve the existing case where non-rule adjustment amount changes under the same rule and must remain only `AdjustmentsChanged`;
- re-fetch exact pushed source/test, inspect diff and verify ancestry against moving `main` before closeout;
- report only executed validation; no managed/native PASS without tooling/runtime evidence.

## Coordination

The original snapshot-delta reason feature/smoke commits are established history; recent exact claim searches found no active follow-up for adjustment rule association. Current Cost/frozen-estimate/signed-zero work and long-running native Solid3d work are disjoint.

## Completion condition

Per-adjustment rule provenance cannot be lost by global token sorting when evidence rows remain alignable, ordinary adjustment-only changes are not mislabeled as provenance changes, focused regression is on current `main`, exact readback/ancestry is verified, and this claim is marked `COMPLETED` with actual validation boundaries.
