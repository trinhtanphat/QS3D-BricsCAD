# Work claim — MTR-05 MeasurementTrace duplicate evidence integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-mtr05-duplicate-evidence-20260813-1808`
- Workstream: `MeasurementTrace / MTR-05` — fail closed on exact duplicate trace evidence
- Claimed UTC: `2026-08-13T11:08:00Z`
- Last updated UTC: `2026-08-13T11:12:00Z`
- Baseline main SHA: `b75c2cc2a8b8ec5934f749eb6cb11ea2b8676522`

## Confirmed defect

Current `MeasurementTraceContract.SnapshotFacts()` and `SnapshotAdjustments()` validated null entries, sorted deterministically and froze the snapshots, but accepted structurally identical duplicate entries. The same contract already rejected duplicate warnings/assumptions, and `MeasurementSnapshot` rejects duplicate measurement identities. Exact duplicate adjustments therefore allowed redundant/ambiguous explanatory evidence in the canonical trace while neighboring canonical structures fail closed on duplicates.

## Reserved files

- `src/QS3D.Core/Measurement/MeasurementTrace.cs`
- `tests/QS3D.Core.SmokeTests/MeasurementTraceContractSmoke.cs`
- this claim file

## Implemented scope

- After canonical sort, `SnapshotFacts()` now rejects adjacent structurally identical facts with `ArgumentException`.
- After canonical sort, `SnapshotAdjustments()` now rejects adjacent structurally identical adjustments with `ArgumentException`.
- Existing comparison/equality fields define exact duplicate identity, so same-source evidence remains valid when any canonical field differs; existing MTR2 ordering coverage continues to exercise adjustments differing only by rule identity.
- Added focused `DuplicateEvidenceFailsClosed()` smoke coverage using separate-but-structurally-equal fact and rule-aware adjustment objects.
- Preserved ordering, equality/hash/canonical serialization, MTR1/MTR2 schema behavior, quantity values, units and all calculation ownership for non-duplicate evidence.
- Did not touch Wall/MTR-03R, Takeoff/report/UI, persistence, BricsCAD/native, or formula/calculation surfaces.

## Coordination / overlap reconciliation

- Claim-only commit: `59f44b21303256d0370efdf13c387f4e743b6cb0` — `chore(agent): claim MTR-05 duplicate trace evidence`.
- Historical MeasurementTrace nullable lane was already `COMPLETED`; MTR-03R Wall Quantity trace projection was also `COMPLETED` and reserved different files.
- Post-claim refresh showed this claim at HEAD before source write.
- After source/regression push, `main` advanced once to `91f44f9d1dbf99bf3b741b2e2e4ff35534d8fcab`; compare from regression commit changed only the unrelated Zone Manager claim file, so neither reserved implementation file was overwritten.

## Implementation commits

- `b98410aa473543791ca392cb6590d3b6a2b84b8a` — `fix(measurement): reject duplicate trace evidence`.
- `e9de0340e19232cc34b960d19f336cdd90e45883` — `test(measurement): guard duplicate trace evidence`.

## Validation actually executed

- Executed: current-`main` refresh before claim, after claim, after implementation and before closeout.
- Executed: exact GitHub diff inspection for both implementation commits.
- Executed: direct current-main readback of the source uniqueness gates and registered `DuplicateEvidenceFailsClosed()` smoke body.
- Executed: GitHub compare `e9de0340e19232cc34b960d19f336cdd90e45883..91f44f9d1dbf99bf3b741b2e2e4ff35534d8fcab`; only the unrelated Zone Manager claim file changed.
- Not executed: GitHub Actions, repository `dotnet build`, Core smoke executable, installed-reference BricsCAD V25 build or licensed BricsCAD runtime qualification. No PASS is claimed for those unexecuted gates.

## Completion

Completed for this bounded source/regression lane: claim-first ownership was published, the current-source defect was confirmed, exact duplicate facts/adjustments now fail closed, focused regression coverage is on `main`, concurrent work was reconciled without force-push/overwrite, remote readback confirmed the landed code, and unexecuted managed/native gates remain explicitly unclaimed.
