# Work claim — Revision canonical element ID fail-closed integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-revision-canonical-id-20260811-2239`
- Registered: `2026-08-11T22:39:45+07:00`
- Baseline main SHA: `fbb7fae24e2fb2086715aba071aa8946e88e47ad`
- Priority: P2 source-proven regression hardening

## Reserved scope

Fix the Core Revision identity-validation mismatch where persisted revision snapshots and `SemanticChangeReviewBuilder` reject leading/trailing whitespace in semantic Element IDs, while `RevisionService.Compare` and `QuantityRevisionReport.Build` currently index raw non-blank IDs. A malformed in-memory/public `RevisionSnapshot` can therefore be interpreted as different Added/Removed identities instead of failing closed at the comparison boundary.

## Expected surfaces

- `src/QS3D.Core/Revisions/RevisionService.cs`
- `src/QS3D.Core/Revisions/QuantityRevisionReport.cs`
- `tests/QS3D.Core.SmokeTests/RevisionRegressionSmoke.cs`
- this claim file for close-out

## Explicit exclusions

- No Revision WPF/UI/code-behind changes; the completed Revision luxury UI lane remains untouched.
- No revision persistence schema/version or XML store changes.
- No quantity calculation/rule semantics changes.
- No BricsCAD/native/runtime changes.
- No GitHub Actions dispatch or workflow edits.

## Validation plan

- Re-fetch the exact current source before implementation and preserve concurrent changes.
- Require canonical non-padded Element IDs in both low-level revision comparison/index boundaries, matching the existing `RevisionSnapshotStore` and `SemanticChangeReviewBuilder` fail-closed contract.
- Add focused smoke regression coverage proving padded Element IDs are rejected by both `RevisionService.Compare` and `QuantityRevisionReport.Build`, including the case where trimming would collide with another element identity.
- Preserve current case-insensitive canonical duplicate detection and numeric overflow behavior.
- Validation in this connector-only lane is source/static readback plus committed deterministic smoke coverage; do not claim a local .NET/BricsCAD execution.

## Coordination

The recently completed Revision luxury UI claim explicitly excluded Core revision arithmetic/snapshots, so this claim is disjoint from that released presentation lane. If a newer active claim reserves the same Core Revision files before implementation, stop and re-scope.

## Completion condition

The source-proven canonical-ID comparison gap is fixed on current `main`, focused regression coverage is committed, final current-main source is re-read, and this claim is marked `COMPLETED` with exact implementation SHAs and validation actually performed.
