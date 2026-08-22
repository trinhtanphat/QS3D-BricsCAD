# Work claim — REV-02A deterministic Measurement Snapshot delta

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-rev02a-measurement-delta-20260813-1325`
- Registered: `2026-08-13T13:25:04+07:00`
- Baseline main SHA: `559e7aa931b83bb93ab574fe6e5273a8e28b5ff0`
- Priority: `REV-02 / P0-P1` — deterministic quantity delta directly above the completed canonical Measurement Snapshot foundation

## Confirmed gap

At registration, `src/QS3D.Core/Measurement` contained canonical `MeasurementTrace` and `MeasurementSnapshot` only; there was no measurement-snapshot delta contract. The existing `RevisionService.Compare` and `QuantityRevisionReport` operate on the older whole-project `RevisionSnapshot` / raw quantity dictionaries. They do not consume frozen `MeasurementTrace` lines and do not retain canonical unit/rule/source provenance per measured line.

Historical `quantity revision small delta integrity` work was explicitly cancelled after confirming the existing revision subsystem's shared `1e-9` tolerance policy; no production behavior change remained from that cancelled claim. This lane did not reopen or modify that policy.

## Reserved scope

Add one pure-Core deterministic comparer/result contract over two already-frozen `MeasurementSnapshot` values.

Implemented behavior:

- classify exact canonical measurement identities as `Added`, `Removed`, `Unchanged`, or `Changed`;
- use the same exact ordinal identity tuple as `MeasurementSnapshot`: `(SemanticIdentity, SourceIdentity, QuantityKey)`;
- retain previous/current `MeasurementTrace` references so every delta row remains traceable to source/rule/unit provenance;
- expose previous/current net values as nullable presence values and a signed delta derived only from existing canonical `NetValue` values;
- classify a shared identity as `Unchanged` only when canonical `MeasurementTrace.Equals` is true, so rule/provenance/fact changes remain visible even when numeric delta is zero;
- fail visibly instead of subtracting unlike units when one shared measurement identity changes unit;
- return rows in deterministic ordinal identity order.

## Expected surfaces

- `src/QS3D.Core/Measurement/MeasurementSnapshotDelta.cs` — added
- `tests/QS3D.Core.SmokeTests/MeasurementSnapshotDeltaSmoke.cs` — added
- `tests/QS3D.Core.SmokeTests/MeasurementSnapshotDeltaRegistration.cs` — added
- this claim file

## Excluded scope

- No edits to `MeasurementSnapshot.cs`, `MeasurementTrace.cs`, `QuantityEngine`, `TakeoffResultWithTrace`, Quantity Rules or measurement calculation paths.
- No edits/replacement of `RevisionService`, `RevisionSnapshot`, `QuantityRevisionReport`, `QuantityReportRevisionService`, their established tolerance policy, or revision persistence.
- No REV-03 reason classification beyond preserving canonical trace data; no geometry-vs-rule-vs-mapping reason inference.
- No rates/cost, BOQ mapping, report/UI/XLSX, persistence/schema, PERF harness, native BricsCAD adapters or LOCAL qualification.
- No second quantity engine or unit conversion path: delta arithmetic consumes canonical snapshot `NetValue` values only and rejects shared-identity unit mismatch.
- No GitHub Actions or native/runtime qualification.

## Coordination

- Claim-only commit: `0918057fb16774f4becd6d2c3d600c57a84b4146`.
- The claim commit was re-fetched as current `main` and baseline-to-claim comparison showed exactly one added claim file before substantive work.
- PERF-02 owned only `tests/QS3D.Core.PerfHarness/Program.cs` and explicitly excluded Measurement Snapshot source; this lane did not touch the perf harness.
- LOCAL-003 and Curtain runtime lanes remained excluded.
- REV-01A was consumed as-is and not edited.
- Historical current-main checks for `REV-02`, `quantity delta`, `measurement delta`, and `MeasurementSnapshotDelta`, plus exact baseline-to-head diffs, found no conflicting current implementation/claim before registration. Large claim-registry responses remain connector-truncated, so no false claim of an exhaustive local `rg` scan is made.

## Completion evidence

- Claim-only commit: `0918057fb16774f4becd6d2c3d600c57a84b4146`.
- Implementation commit: `c5cb213d89e615eae9ae4f3213d6d7d09936fe48`.
- `main` was re-fetched immediately after push and resolved exactly to the implementation commit.
- Remote source readback at the implementation SHA verified:
  - `src/QS3D.Core/Measurement/MeasurementSnapshotDelta.cs` blob `9f5c5e270293d7dd73574c7ab389cc19860b3194`;
  - `tests/QS3D.Core.SmokeTests/MeasurementSnapshotDeltaSmoke.cs` blob `93041131ba1cf6278749a0b0bdb1524e122cb450`;
  - `tests/QS3D.Core.SmokeTests/MeasurementSnapshotDeltaRegistration.cs` blob `aeee7c352c386ba507d8e47d38ffa4f0ff4bc5e3`.
- Static contract verification confirmed a deterministic two-pointer merge over already-sorted frozen snapshots, exact-ordinal identity matching, trace-equality classification, nullable previous/current presence, signed delta with signed-zero normalization, preserved canonical trace references, and fail-closed shared-identity unit mismatch.
- Focused smoke source covers Added/Removed/Unchanged/Changed ordering, signed deltas, trace provenance retention, rule-version-only change with zero numeric delta, exact-ordinal case-distinct identity, unit mismatch rejection, and null argument rejection.
- Registration was remotely verified through the repository's existing `ModuleInitializer` smoke pattern.
- Executable managed smoke: `NOT_RUN` in this connector-only environment because no usable local repository/.NET execution path is available.
- GitHub Actions: `NOT_RUN` by policy/request boundary.
- BricsCAD V25/V26 native qualification: `NOT_APPLICABLE` to this pure-Core contract and not claimed.

## Remaining gates / handoff

- A checkout with .NET may execute `dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj` to convert registered smoke source into executable PASS evidence.
- REV-03 deterministic reason classification, persistence, report/UI projections, rate/cost work and native adapters remain separate future claims.

## Completion condition

Satisfied: a pure-Core deterministic Measurement Snapshot delta contract plus focused smoke/registration is pushed on `main`; existing revision and quantity engines remain untouched; the implementation was remotely re-fetched and reviewed; actual validation and remaining gates are recorded above.
