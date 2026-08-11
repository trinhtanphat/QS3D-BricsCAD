# Work claim — generated native source-recognition ownership audit

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-generated-source-recognition-20260811-1944`
- Registered: `2026-08-11T19:44:33+07:00`
- Baseline main SHA: `315b26440c35bbe1af244254aa3e68a7ed4c7b45`
- Priority: continue the source-safe BLT/B4D recognition hardening after native generated-output XData exclusion landed; prevent QS3D-generated CAD artifacts from being recaptured as authoritative semantic sources while avoiding false exclusion of legitimate source entities

## Reserved scope

Audit and, only where current source proves a defect, harden the generated-native ownership classification used by semantic source recognition. Verify that every current QS3D generated-output RegApp family is represented by `GeneratedNativeSourceGuard`, that live CAD snapshots used for recognition carry this classification into Core eligibility, and that invalid/generated ownership is rejected before semantic capture without introducing a second ownership model.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/Cad/GeneratedNativeSourceGuard.cs`
- `src/QS3D.BricsCAD.V25/Cad/EntitySnapshotReader.cs` only if a confirmed classification gap exists
- `src/QS3D.Core/Model/EntitySnapshot.cs` and `src/QS3D.Core/Recognition/EntitySnapshotCaptureEligibility.cs` only if a confirmed recognition-boundary defect exists
- existing generated ownership services/builders as read-only evidence for RegApp coverage
- focused recognition/generated-source preflight or Core smoke coverage only when needed to lock a confirmed defect
- this claim file for close-out status

## Excluded scope

- No Direct Draw/Create Similar selection-ownership or Family activation workflow reserved by `2026-08-11-chatgpt-web-create-similar.md`.
- No Core mutation atomicity under Navigation/Review/Interchange/Rules reserved by `2026-08-11-chatgpt-web-core-mutation-atomicity.md`.
- No Workspace multi-selection policy, Room Finish mutation/regeneration, modeless viewer project identity, Material Catalog product behavior, or LOCAL-003 Level Z-chain work.
- No broad recognition redesign, new semantic model, geometry-builder rewrite, BricsCAD V25 runtime qualification, GitHub Actions dispatch, release, signing or installer work.

## Validation plan

- Re-fetch current `main` and claims before every implementation write.
- Enumerate current native generated-output ownership RegApp writers and compare them against `GeneratedNativeSourceGuard` rather than guessing from feature names.
- Trace all current semantic-recognition snapshot capture paths to ensure generated ownership classification reaches `EntitySnapshotCaptureEligibility` before capture.
- Add a focused static/smoke regression only for a confirmed source gap; preserve fail-closed behavior for malformed/legacy generated markers.
- Inspect pushed diffs/full files and remote ancestry; do not claim V25 runtime or GitHub Actions execution.

## Coordination

Current neighboring claims reserve Create Similar, Core mutation atomicity, Workspace multi-policy, Room Finish mutation safety, modeless viewer identity and LOCAL-003 Level Z-chain. This lane is limited to generated-output classification in the recognition input boundary and does not take command-side semantic selection/activation ownership.

## Completion condition

All current generated ownership families and recognition snapshot paths are either proven covered with no product-source change, or focused source/test/preflight fixes are pushed to current `main`; the exact audited outcome is recorded here and this claim is marked `COMPLETED` or `RELEASED` with any V25-only residual explicitly unclaimed.
