# Project-wide remote-safe audit — 2026-08-16

Status: ACTIVE
Agent/session: chatgpt-gpt56sol
Baseline: `main@b7c350191d4c560a3f4960e64a04fcb9d7f4f896`
Branch: `agent/chatgpt-gpt56sol/project-wide-audit-20260816`

## Owner request
Review the whole repository again, identify verified remote-safe defects, fix them, update regression coverage, and push the work on a dedicated branch.

## Collision boundary
This audit must not duplicate or take over currently reserved/open lanes. Existing active/open PR and issue scopes remain excluded unless the owner separately authorizes integration/review of that exact lane. If a discovered defect overlaps an existing reservation, record/skip it and move to a non-overlapping defect.

## Audit focus
- deterministic Core correctness and numeric edge cases;
- parser/serialization strictness and culture invariance;
- path/archive/input fail-closed behavior;
- stale-state/document-affinity safety in remote-verifiable source;
- deterministic regression coverage and source guards where appropriate.

## Exclusions
- no direct `main` writes or merge;
- no force push;
- no takeover of other agent branches/PRs;
- no licensed BricsCAD runtime claims;
- no manual GitHub Actions dispatch unless separately authorized.

## Completion rule
Each implemented defect must be independently reproducible from current source, have bounded scope, include regression evidence, and remain on this branch/PR until owner-authorized integration.

## Verified fixes delivered on this audit branch

### Curtain wall layout arithmetic underflow
`CurtainWallLayoutPlanner` accepted strictly positive finite dimensions/frame widths but could silently round a nonzero division result to zero. The guarded divide path now fails closed on nonzero-to-zero underflow, with deterministic smoke coverage for positive mullion/transom half-width loss while preserving ordinary and legitimate zero-width internal-frame cases.

### Curtain opening/frame coordinate-resolution collapse
`CurtainFrameOpeningPlanner` accepted strictly positive opening/frame dimensions whose endpoint arithmetic collapsed back onto the starting coordinate (for example `1e16 + 1d == 1e16`). That left a semantic positive extent which the clipping arithmetic observed as zero. Opening base bounds and positive clearances now require representable outward movement, frame right/top bounds require strict representable ordering, and deterministic smoke coverage exercises horizontal/vertical size collapse, clearance collapse, frame collapse, plus the ordinary four-fragment interruption path.

### Alternate curtain opening-frame planner coordinate collapse
`CurtainWallOpeningFramePlanner` had the same representability gap in its mutable opening DTO path: positive finite widths/heights could collapse their right/top endpoints back onto the starting coordinate, and a positive clearance could be rounded away on one side while still being treated as applied. Rectangle validation now requires strict representable right/top movement, positive clearance must expand both sides on each axis, and deterministic smoke coverage preserves the ordinary interruption/area contract while rejecting collapsed opening, frame and clearance inputs.

### Curtain detail generated-rectangle collapse and area underflow
`CurtainWallDetailPlanner` could generate a positive-width/height frame whose endpoint rounded back onto its start at large coordinates, and its detail-area multiplication accepted nonzero dimensions whose product underflowed to zero. Generated rectangles now require representable right/top movement, detail area multiplication fails closed on nonzero-to-zero underflow, and `CurtainWallRect.AreaM2` no longer reports zero for an unrepresentable nonzero area. Regression coverage exercises horizontal/vertical generated-frame collapse, direct rectangle-area underflow, per-panel area underflow, and an ordinary detail plan.

## Owner-authorized integration follow-up
The validated curtain fixes above are now being consolidated on `integration/open-pr-cleanup-20260816-r1` together with other non-overlapping green bugfix lanes. The integration candidate is rebuilt from current `main` and intentionally keeps current-main versions of aggregate preflight and updater trust-boundary files, so the older audit carrier cannot reintroduce stale CI/updater content while its 19 intended curtain/audit files are preserved.
