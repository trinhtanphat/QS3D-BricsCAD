# Work claim — REV-02A deterministic Measurement Snapshot delta

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-rev02a-measurement-delta-20260813-1325`
- Registered: `2026-08-13T13:25:04+07:00`
- Baseline main SHA: `559e7aa931b83bb93ab574fe6e5273a8e28b5ff0`
- Priority: `REV-02 / P0-P1` — deterministic quantity delta directly above the completed canonical Measurement Snapshot foundation

## Confirmed gap

Current `src/QS3D.Core/Measurement` contains canonical `MeasurementTrace` and `MeasurementSnapshot` only; there is no measurement-snapshot delta contract. The existing `RevisionService.Compare` and `QuantityRevisionReport` operate on the older whole-project `RevisionSnapshot` / raw quantity dictionaries. They do not consume frozen `MeasurementTrace` lines and do not retain canonical unit/rule/source provenance per measured line.

Historical `quantity revision small delta integrity` work was explicitly cancelled after confirming the existing revision subsystem's shared `1e-9` tolerance policy; no production behavior change remains from that cancelled claim. This lane does not reopen or modify that policy.

## Reserved scope

Add one pure-Core deterministic comparer/result contract over two already-frozen `MeasurementSnapshot` values.

The contract will:

- classify exact canonical measurement identities as `Added`, `Removed`, `Unchanged`, or `Changed`;
- use the same exact ordinal identity tuple as `MeasurementSnapshot`: `(SemanticIdentity, SourceIdentity, QuantityKey)`;
- retain previous/current `MeasurementTrace` references so every delta row remains traceable to source/rule/unit provenance;
- expose previous/current net values explicitly as nullable presence values and a signed delta derived only from existing canonical `NetValue` values;
- classify a shared identity as `Unchanged` only when canonical `MeasurementTrace.Equals` is true, so rule/provenance/fact changes remain visible even when numeric delta is zero;
- fail visibly instead of subtracting unlike units when one shared measurement identity changes unit;
- return rows in deterministic ordinal identity order.

## Expected surfaces

- `src/QS3D.Core/Measurement/MeasurementSnapshotDelta.cs` — new file only
- `tests/QS3D.Core.SmokeTests/MeasurementSnapshotDeltaSmoke.cs` — new focused smoke
- `tests/QS3D.Core.SmokeTests/MeasurementSnapshotDeltaRegistration.cs` — new ModuleInitializer registration
- this claim file

## Excluded scope

- No edits to `MeasurementSnapshot.cs`, `MeasurementTrace.cs`, `QuantityEngine`, `TakeoffResultWithTrace`, Quantity Rules or measurement calculation paths.
- No edits/replacement of `RevisionService`, `RevisionSnapshot`, `QuantityRevisionReport`, `QuantityReportRevisionService`, their established tolerance policy, or revision persistence.
- No REV-03 reason classification beyond preserving canonical trace data; no geometry-vs-rule-vs-mapping reason inference.
- No rates/cost, BOQ mapping, report/UI/XLSX, persistence/schema, PERF harness, native BricsCAD adapters or LOCAL qualification.
- No second quantity engine or unit conversion path: delta arithmetic consumes canonical snapshot `NetValue` values only and rejects shared-identity unit mismatch.
- No GitHub Actions or native/runtime qualification.

## Validation plan

- Re-fetch `main` after this claim-only commit and verify the claim commit is an ancestor of current `main`.
- Recheck recent/new `REV-02`, `quantity delta`, Measurement and revision claims plus baseline-to-claim file changes before source work.
- Focused smoke source will cover deterministic row order, added/removed/unchanged/changed classification, signed deltas, rule-only change with zero numeric delta, exact-ordinal identity behavior, unit-mismatch fail-closed behavior, and null argument rejection.
- Re-fetch implementation files from the pushed implementation SHA and verify exact remote blobs on current `main`.
- Connector-only source inspection is not executable managed evidence; smoke execution will be recorded `NOT_RUN` unless a real checkout/.NET execution path becomes available.

## Coordination

- PERF-02 currently owns `tests/QS3D.Core.PerfHarness/Program.cs` and explicitly excludes Measurement Snapshot source; this claim does not touch the perf harness.
- LOCAL-003 and Curtain runtime lanes remain excluded.
- The completed REV-01A contract is consumed as-is and not edited.
- Current-main history checks for `REV-02`, `quantity delta`, `measurement delta`, and `MeasurementSnapshotDelta` returned no matching current implementation/claim. Recent `f6ebb9bd... -> 38c22f...` changes touched only Level runtime/probe surfaces and the existing perf harness; `38c22f... -> 559e7aa...` touched only the PERF-02 claim close/update.
- Large claim-registry responses remain connector-truncated, so ownership proof uses current Git history/targeted claim reads, recent-commit scan, current source tree and exact baseline-to-head diffs rather than falsely treating disabled code-search as exhaustive.

## Completion condition

A pushed pure-Core deterministic Measurement Snapshot delta contract plus focused smoke/registration is present on current `main`; existing revision/quantity engines remain untouched; the implementation is remotely re-fetched and reviewed; and this claim is updated to `COMPLETED` with exact implementation SHA plus validation actually executed and remaining gates.
