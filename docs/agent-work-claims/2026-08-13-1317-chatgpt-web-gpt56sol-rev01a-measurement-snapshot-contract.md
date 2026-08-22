# Work claim — REV-01A canonical Measurement Snapshot contract

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-rev01a-measurement-snapshot-20260813-1317`
- Registered: `2026-08-13T13:17:30+07:00`
- Baseline main SHA: `94f6e5b7e9c57238a9eaa900210ea90309705aad`
- Priority: `REV-01 / P0-P1` — measurement snapshot foundation after canonical MeasurementTrace and rule provenance

## Reserved scope

Add one pure-Core immutable measurement snapshot contract that freezes an already-computed set of canonical `MeasurementTrace` records. The snapshot must not recalculate quantities and must not replace the existing general-purpose `RevisionSnapshot` / `RevisionService` system.

The contract will:

- detach the caller-owned trace sequence into an immutable/read-only snapshot;
- reject null trace entries and duplicate measurement identities rather than silently overwrite them;
- use deterministic ordering for the same measurement set independent of caller order or culture;
- retain explicit semantic/source/quantity identity, net value/unit and optional rule id/version through the canonical `MeasurementTrace` records;
- expose deterministic canonical snapshot text suitable for later REV-02 comparison/fingerprinting without adding time-dependent data.

## Expected surfaces

- `src/QS3D.Core/Measurement/MeasurementSnapshot.cs` — new file only
- `tests/QS3D.Core.SmokeTests/MeasurementSnapshotContractSmoke.cs` — new focused smoke
- `tests/QS3D.Core.SmokeTests/MeasurementSnapshotContractRegistration.cs` — new ModuleInitializer registration, following existing smoke-registration precedent
- this claim file

## Excluded scope

- No edits to `src/QS3D.Core/Measurement/MeasurementTrace.cs` or `MeasurementTraceContractSmoke.cs`; the concurrent MTR-02 adjustment-rule provenance lane owns that surface.
- No edits to `QuantityRule`, `QuantityRuleEngine`, rule evaluation or persistence.
- No edits/replacement of `RevisionService`, `RevisionSnapshot`, `RevisionSnapshotStore`, semantic revision comparison or existing revision persistence.
- No REV-02 quantity delta logic, reason classification, report/UI/XLSX projection, cost/rates, native BricsCAD adapters or LOCAL-003 qualification.
- No second quantity calculation path: snapshot content consumes existing canonical traces verbatim.
- No GitHub Actions or native/runtime qualification.

## Validation plan

- Re-fetch `main` after this claim-only commit and verify this claim commit is on current lineage.
- Recheck newly published claims and recent REV/MeasurementSnapshot/MTR ownership for overlap before source work.
- Focused deterministic smoke coverage will prove caller-list isolation, stable ordering/canonical text across input order/culture, duplicate-identity fail-closed behavior, and preservation of trace net/unit/rule/source provenance.
- Re-fetch the implementation commit and exact files from current `main` after push.
- Because this environment has no usable local checkout/.NET build path, executable smoke/native PASS will not be claimed unless a real executable gate becomes available during the lane.

## Coordination

- Current source inspection showed `src/QS3D.Core/Measurement` contained `MeasurementTrace.cs` but no `MeasurementSnapshot` contract before implementation.
- The existing `RevisionSnapshot` captures mutable whole-project element/property/raw-quantity state and remains unchanged; this lane is a measurement-specific frozen projection over canonical traces.
- The concurrent MTR-02 adjustment-rule claim reserved `MeasurementTrace.cs` plus its smoke; this lane avoided those files and did not alter trace semantics.
- Baseline-to-claim compare showed only the unrelated Curtain P11 claim arrived concurrently; no REV/MeasurementSnapshot source or test overlap was introduced.

## Completion evidence

- Claim-only commit: `03923d284c4f6351fbdee3c96892200cbc04a0e4`.
- Implementation commit: `1e9e38cfa475ff15e2636d6e1e37806b2a9a01e5`.
- Final product/source SHA verified before close-out: `1e9e38cfa475ff15e2636d6e1e37806b2a9a01e5`.
- Remote verification: `main` resolved to the implementation commit after push; all three reserved files were fetched back at that SHA with blob SHAs `6d90bfc06cbcd9a39ffedcb562d5191a3196c03f`, `ddb752df3c6d2a907d0bbb7373ac2700ece90472`, and `e1f43f0e6444a49c497fa6998b4187aefc77e3da`.
- Static contract verification: snapshot constructor detaches/sorts the input list, rejects null/duplicate measurement identities, preserves immutable canonical `MeasurementTrace` objects, and canonical serialization embeds each trace's existing `ToCanonicalString()` without recalculating quantities.
- Test registration verification: the new focused smoke is registered through an existing repository `ModuleInitializer` pattern and the smoke project is SDK-style `net8.0`; Core is SDK-style `netstandard2.0`, so the new source file is included by default.
- Executable managed smoke: `NOT_RUN` in this connector-only environment because no usable local repository/.NET execution path is available.
- GitHub Actions: `NOT_RUN` by policy/request boundary.
- BricsCAD V25/V26 native qualification: `NOT_APPLICABLE` to this pure-Core contract and not claimed.

## Remaining gates / handoff

- A local/CI environment may execute `dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj` to turn the registered regression from source evidence into executable PASS evidence.
- REV-02 deterministic quantity delta, persistence, report/UI projections, rates/cost and native adapters remain separate future claims.

## Completion condition

Satisfied: the pure-Core frozen measurement snapshot contract plus focused deterministic smoke/registration is pushed on `main`, no existing revision or measurement calculation engine was modified, and actual validation plus remaining execution gates are recorded above.
