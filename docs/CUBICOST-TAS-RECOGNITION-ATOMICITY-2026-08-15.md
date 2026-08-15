# Cubicost TAS benchmark — recognition review atomicity

Date: 2026-08-15  
Claim: #1679  
Base: `6d0bde12266f3839752818ffeeb261852b73ae4e`

## Why this lane exists

Cubicost TAS-style takeoff is useful as a product benchmark because recognition is not valuable merely when it produces a suggestion; the QS must be able to review and commit a coherent set of recognized source objects without silently leaving half of a reviewed batch in the project after a late failure.

QS3D already had the important foundations before this lane:

- deterministic `RecognitionEngine` scoring;
- project-specific exact layer mappings;
- confidence, margin and capture-readiness review gates;
- active-DWG and project-identity checks;
- live CAD-handle refresh before capture;
- ownership collision checks;
- modeless `RecognitionWindow` with Locate / selected apply / confident apply;
- `QS3DRECOGNIZE`, `QS3DRECOGNIZEAUTO` and `QS3DB4D` entrypoints.

The missing integrity property was the transaction boundary. The previous UI and AUTO/B4D flows invoked `SemanticCaptureService.CaptureSnapshot` one row at a time. `CaptureSnapshot` correctly rolled back one failing row, but it could not undo earlier successful rows from the same reviewed batch.

## Implemented contract

The V25 adapter now uses `RecognitionApplyBatchService` with two explicit phases.

### 1. Preflight / review phase — no semantic capture

Every candidate to be committed is revalidated against the current drawing and canonical current project:

- source DWG must still be active;
- project id must still match the project that produced the review;
- project `ChangeVersion` is pinned for the complete preflight;
- reviewed CAD handle must still resolve to exactly one live snapshot;
- recognition is recomputed against the live snapshot;
- suggested category must still match the reviewed category;
- capture readiness must still be true;
- conflicting semantic source ownership is rejected;
- duplicate handles inside one apply batch are rejected/filtered;
- the batch is bounded to the same 250,000-row ceiling used by recognition input.

Manual review uses strict preflight: one invalid requested row rejects the requested batch before mutation.

AUTO/B4D uses best-effort preflight: stale or conflicting candidates can be classified as skips before mutation, while valid candidates form the accepted mutation set.

### 2. Commit phase — one semantic transaction

Immediately before the first semantic capture, QS3D captures one `ProjectStateSnapshot` for the canonical project. It then:

1. captures every accepted live snapshot;
2. verifies each capture produced exactly one semantic owner of the expected category;
3. records `recognition.apply` audit events;
4. records AUTO/B4D `recognition.skip` audit events.

Any exception in capture, ownership verification or audit restores the outer pre-batch snapshot. This means a mutation-stage failure cannot leave earlier rows from the same accepted batch committed.

`RecognitionWindow` now receives one `Func<IReadOnlyList<RecognitionResult>, int>` batch callback. It no longer loops a per-row mutation callback, so selected/confident apply cannot accidentally recreate the old partial-success semantics in the UI layer.

## What this does not claim

This lane does **not** claim full Cubicost TAS parity. It does not copy proprietary Cubicost UI/rules and does not add a second recognition engine. It also does not touch:

- TBQ/cost workspace work reserved by #1674;
- Quantity Insight BREP/highlight/locate reserved by #1669;
- MEP review/profile work from #1666;
- vendor-neutral commercial contracts intended for `QS3D-Platform`;
- recognition scoring/default-rule changes.

The product direction remains: use Cubicost as a workflow benchmark, then implement QS3D-native, deterministic and auditable behavior around BricsCAD public APIs and QS3D's existing semantic model.

## Validation boundary

Source-safe regression is pinned by:

```text
python scripts/preflight-recognition-atomic-batch.py
```

The guard verifies that:

- `RecognitionWindow` uses one batch callback and has no `_apply(row)` loop;
- review, AUTO and B4D route through the batch coordinator;
- preflight contains no semantic capture call;
- the commit ordering remains outer snapshot -> semantic capture -> audit -> restore-on-error;
- project-version, live-handle and semantic-ownership gates remain present.

Licensed BricsCAD V25 runtime behavior is not inferred from source inspection. The existing LOCAL-013 B4D/recognition qualification remains the matching local runtime lane; this change introduces no new native API or proprietary-data requirement. A future exact-SHA local run should additionally inject or reproduce a late accepted-row failure and verify that pre/post project semantic state and audit state are identical after the reported batch rollback.
