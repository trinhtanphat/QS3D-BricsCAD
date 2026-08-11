# Work claim — EntitySnapshot handle canonicalization

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-entity-snapshot-handle-canonicalization`
- Registered: `2026-08-11T22:34:00+07:00`
- Baseline main SHA: `6c1eb17c4dc87a571a6e6afb9627d356a64ecfc4`
- Priority: keep CAD handle identity canonical across snapshot/recognition boundaries.

## Confirmed defect

`EntitySnapshot` rejects blank `handle` values but stores a nonblank handle unchanged, while the same constructor already canonicalizes `EntityType`. `RecognitionResult.Handle` exposes `Snapshot.Handle` directly. A snapshot constructed with a padded CAD handle such as `" 1A2B "` therefore survives recognition with a different textual identity from canonical `"1A2B"` even though generated-handle ownership lookups defensively trim handles before matching.

This leaves the public model boundary able to represent one CAD handle with multiple whitespace-distinct values and pushes normalization burden onto downstream consumers.

## Reserved scope

- `src/QS3D.Core/Model/EntitySnapshot.cs`
- focused existing Core smoke coverage for `EntitySnapshot` / recognition handle identity
- this claim file

## Intended contract

- `EntitySnapshot.Handle` stores the trimmed nonblank CAD handle supplied at construction.
- `RecognitionResult.Handle` therefore exposes the canonical handle without changing recognition scoring or category behavior.
- Existing canonical handles remain byte-for-byte unchanged.
- Do not modify `RecognitionEngine.cs`, generated ownership diagnostics, adapter extraction, or currently active recognition workstreams.

## Validation plan

- Re-fetch `main`, reserved blobs and recent claims immediately after reservation.
- Add focused Core smoke/assertion proving padded input becomes canonical and remains the same through `RecognitionResult.Handle`.
- Preserve existing entity-type/proxy safety coverage.
- No GitHub Actions dispatch and no BricsCAD V25 runtime claim.

## Completion condition

Padded CAD handles can no longer escape the `EntitySnapshot` model boundary, focused regression source is merged, and this claim is closed with exact implementation SHAs and truthful validation scope.
