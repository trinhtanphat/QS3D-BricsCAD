# Work claim — MTR-04 adjustment rule provenance association

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-mtr04-adjustment-rule-association-20260813`
- Registered: `2026-08-13T19:07:00+07:00`
- Completed: `2026-08-13T19:10:00+07:00`
- Baseline main SHA: `9f7f5392066f5af3cb119b9a6842098180b271b8`
- Priority: P0 explainable-measurement/revision evidence integrity. `MeasurementSnapshotDeltaReasonClassifier.AdjustmentRuleProvenanceChanged()` reduced adjustment rule provenance to a sorted multiset of `RuleId/RuleVersion` tokens. Two distinct adjustment evidence rows could therefore exchange the same rule-token multiset while the classifier reported only `AdjustmentsChanged` and missed `RuleProvenanceChanged`.

## Reserved scope

- `src/QS3D.Core/Measurement/MeasurementSnapshotDeltaReason.cs`
- `tests/QS3D.Core.SmokeTests/MeasurementSnapshotDeltaReasonSmoke.cs`
- this claim file for closeout

## Result

- Implementation: `0051efd8d37612ad3345d3d37c8c92ebacba55a8` (`fix(measurement): preserve adjustment rule association`).
  - Existing global rule-token detection remains first and unchanged in meaning.
  - When global rule tokens are unchanged, the classifier now compares rule assignments only if the canonical non-rule adjustment evidence (`Kind`, `Amount`, `Unit`, `Reason`, `SourceIdentity`) is otherwise identical.
  - This makes an actual rule reassignment visible without falsely labeling ordinary amount/reason/source evidence changes as provenance changes.
- Regression: `47b975d0976057ae6e10303d4f15ae1e59d21b95` (`test(measurement): guard adjustment rule association`).
  - Two distinct adjustments that swap `rule-a` / `rule-b` now require `RuleProvenanceChanged` plus `AdjustmentsChanged`.
  - An amount-only change that retains the same rule remains `AdjustmentsChanged` only.
  - The smoke fixture helper now derives gross value from canonical `none` adjustments when gross is omitted, keeping the pre-existing reason smoke compatible with the already-landed no-rounding reconciliation contract instead of constructing invalid traces.

## Validation actually performed

- Claim was pushed alone and `main` was refreshed before source mutation; recent exact MeasurementSnapshot/MeasurementTrace reason claim searches found no overlapping active follow-up.
- Exact implementation diff was re-read from GitHub and changes only adjustment rule-provenance classification helpers; no snapshot-delta arithmetic, trace schema or reason ordering changed.
- Exact regression diff was re-read from GitHub and contains the swap-association case, amount-only negative control, and test-fixture gross reconciliation update.
- At verification time `main` was exactly `47b975d0976057ae6e10303d4f15ae1e59d21b95`, so the implementation and regression were both on the remote head before closeout.
- Environment check found Python 3.13.5 and no `dotnet`, `csc`, `mcs`, `msbuild` or `xbuild`; managed smoke execution was therefore unavailable here. No managed-build PASS, GitHub Actions PASS or licensed BricsCAD runtime PASS is claimed.

## Excluded scope preserved

- no changes to `MeasurementTrace`, `MeasurementSnapshot`, or `MeasurementSnapshotDelta` arithmetic/schema;
- no new causal inference from geometry/properties/mapping;
- no Cost/CST-04 work, signed-zero lanes, report/UI/export, BricsCAD native adapters, sibling Platform migration, GitHub Actions or native qualification.

## Completion condition

Satisfied for source/static scope: per-adjustment rule provenance cannot be lost by global token sorting when evidence rows remain alignable; ordinary adjustment-only changes are not mislabeled as provenance changes; focused regression and compatible fixture construction are on remote `main`; exact diffs were verified; remaining managed/native execution gates are explicitly unclaimed.
