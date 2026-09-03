# Quantity report revision nested-state stability

## Purpose

`QuantityReportRevisionService.Capture` publishes one immutable review object that combines a semantic `RevisionSnapshot` with authoritative BQ detail rows. Those two views must describe one stable nested project-state generation, not merely the same parent `ProjectState.ChangeVersion` value.

Nested `ProjectFamily` and `ProjectElement` state can change without incrementing the parent project revision. A single semantic pass followed by one BQ pass can therefore observe different generations while a final `ChangeVersion` check still succeeds.

## Admission contract

Capture performs two complete materialization passes. Each pass independently captures the semantic revision and the authoritative BQ detail rows. The project identity and parent `ChangeVersion` are rebound after each pass.

The two semantic revisions are validated and compared through the existing `RevisionService.Compare` authority. The ordered immutable BQ row snapshots use exact row equality, including exact string identity/text and exact numeric values; the display-oriented quantity tolerance used by revision review diffs is not an admission tolerance.

Only the second pass may be published, and only after both complete passes agree. There is no retry loop: any detected nested-state movement fails closed so a caller can deliberately start a fresh capture against a new stable generation.

## Deterministic regression

`QuantityReportRevisionReviewSmoke.NestedMutationDuringCaptureFailsClosed` replaces one element's `Properties` dictionary with a deterministic enumerator that changes `FamilyId` after `RevisionService` has copied the element's semantic family identity. The parent project `ChangeVersion` does not move. The historical single-pass algorithm could therefore combine the old semantic family identity with BQ rows from the new family generation.

The corrected service rejects that mixed-time capture. Existing ordinary capture/compare cases remain stable positive controls and continue proving the service does not mutate live project state.

## Validation

Run:

```text
python scripts/preflight-quantity-report-revision-nested-stability.py
dotnet build src/QS3D.Core/QS3D.Core.csproj -c Release
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

Hosted validation is deterministic Core/source evidence only. Licensed BricsCAD runtime is not applicable to this carrier.
