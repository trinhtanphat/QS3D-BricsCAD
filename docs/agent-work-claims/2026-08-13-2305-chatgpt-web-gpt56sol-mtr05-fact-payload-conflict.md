# Work claim — MTR-05 fact payload conflict

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-mtr05-fact-payload-conflict-20260813-2305`
- Registered: `2026-08-13T23:05:00+07:00`
- Completed: `2026-08-13T23:25:00+07:00`
- Baseline main SHA: `24e604107a5d03dde70d234b2a61d1443e5a2313`
- Claim merge SHA: `e263fda5b96e7318eb86fd4d46c75b7dc2b3adf0` via PR #1084
- Implementation SHA: `5cddc992383db8a69f8f3a7c42e457b62e8a150d`
- Priority: `P0` quantity-trust integrity.

## Confirmed defect

The completed exact-duplicate MTR-05 lane rejected only structurally equal `MeasurementTraceFact` entries. Because canonical ordering groups facts by `Name` and `SourceIdentity` before `Unit` and `Value`, two facts could still share one ordinal `(Name, SourceIdentity)` evidence identity while carrying conflicting value or unit payloads.

## Completed scope

- `MeasurementTraceContract.SnapshotFacts()` now treats ordinal `(Name, SourceIdentity)` as fact evidence identity after canonical sort.
- Exact structural duplicates still fail closed through the existing duplicate guard/message.
- Same-identity facts whose `Value` or `Unit` differs now fail closed as conflicting payloads.
- Facts remain valid when either fact name or source identity differs.
- Adjustment identity/payload semantics are unchanged.
- Equality, hashing, canonical ordering, MTR1/MTR2 serialization, quantity formulas and calculation ownership are unchanged for valid traces.

## Regression coverage

`tests/QS3D.Core.SmokeTests/MeasurementTraceContractSmoke.cs` extends `DuplicateEvidenceFailsClosed()` with focused coverage for:

- same fact identity with different value => `ArgumentException`;
- same fact identity with different unit => `ArgumentException`;
- same source with a different fact name remains valid;
- same fact name from a different source identity remains valid;
- the pre-existing exact duplicate fact/adjustment cases remain present.

## Coordination / reconciliation

- Claim-first reservation was published alone through PR #1084 and merged to `main` at `e263fda5b96e7318eb86fd4d46c75b7dc2b3adf0` before source changes.
- After the claim landed, concurrent work continued on Quantity Insight UI, Floor preflight and NETLOAD lifecycle surfaces; none reserved either MeasurementTrace file.
- Source/test work was staged on an agent branch, then reconciled against current `main` SHA `31b47d780911673321d36dbc11b527b01e2cb891` by constructing a tree that replaced only the two reserved blobs.
- Immediately before publishing, `main` was re-read and still matched that parent; `main` was advanced to `5cddc992383db8a69f8f3a7c42e457b62e8a150d` with `force=false`.
- Remote commit readback confirms the implementation commit changes only the reserved source and smoke files: source `+11/-1`, smoke `+47/-0`.

## Validation actually executed

- Refreshed `main` and MTR-05 commit history before implementation; no newer overlapping MTR-05 claim was present.
- Read back the staged source invariant and focused regression body from the agent branch before publication.
- Read back implementation commit `5cddc992383db8a69f8f3a7c42e457b62e8a150d` from GitHub and inspected its exact two-file diff.
- Re-read remote `main` after publication; the implementation commit was current HEAD at the validation checkpoint.
- Queried combined commit status for the implementation SHA; GitHub returned no attached status contexts.
- Not executed: GitHub Actions, repository `dotnet build`, Core smoke executable, installed-reference BricsCAD build, or licensed BricsCAD runtime. No PASS is claimed for those unexecuted gates.

## Excluded scope preserved

- Adjustment identity/payload policy.
- Quantity formulas, projections, persistence, reports, UI, BricsCAD/native behavior.
- Other current agent claims.

## Completion

Completed for this bounded MTR-05 lane: conflicting `MeasurementTraceFact` payloads for one ordinal `(Name, SourceIdentity)` evidence identity now fail closed, focused regression coverage is on `main`, concurrent work was reconciled without force-push/overwrite, and unexecuted managed/native gates remain explicitly unclaimed.