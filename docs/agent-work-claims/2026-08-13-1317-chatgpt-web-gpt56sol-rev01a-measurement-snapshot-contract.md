# Work claim — REV-01A canonical Measurement Snapshot contract

- Status: `ACTIVE`
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

- Current source inspection shows `src/QS3D.Core/Measurement` contains `MeasurementTrace.cs` but no `MeasurementSnapshot` contract.
- The existing `RevisionSnapshot` captures mutable whole-project element/property/raw-quantity state and is intentionally left unchanged; this lane is a measurement-specific frozen projection over canonical traces.
- The current MTR-02 adjustment-rule claim explicitly excludes Measurement Profile, persistence and revision work while reserving `MeasurementTrace.cs` plus its smoke; this claim avoids those files and does not alter trace semantics.
- Recent repository history/search shows no `REV-01`, `MeasurementSnapshot` or `measurement snapshot` claim/implementation before registration. Historical revision-snapshot hardening lanes concern the existing mutable revision subsystem and are not taken over here.

## Completion condition

A pushed Core contract plus focused deterministic smoke/registration is present on current `main`, with no existing revision/measurement calculation engine modified, and this claim is updated to `COMPLETED` with implementation SHA and actual validation evidence.
