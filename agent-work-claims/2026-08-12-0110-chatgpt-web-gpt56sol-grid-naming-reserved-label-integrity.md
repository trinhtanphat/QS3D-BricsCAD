# Work claim — Grid naming reserved-label integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-grid-naming-reserved-label-integrity`
- Registered: `2026-08-12T01:10:00+07:00`
- Completed: `2026-08-12T01:16:00+07:00`
- Baseline main SHA observed: `388de3818354b7e0849fc82bca896ea92cb7b49b`
- Claim commit: `4ba60c002c51d1d154e3cd8f49e4c8d88a657527`
- PR: `#600`
- Squash merge on `main`: `094edd329008ec376e3d47bf49f901447bb601c7`
- Priority: P1 — deterministic Core semantic-integrity / mutation-atomicity defect.

## Defect closed

`GridNamingService.Renumber(...)` built a case-insensitive `reservedLabels` set from non-target Grid elements but ignored the return value of `HashSet.Add`. Two non-target Grids with the same trimmed label were silently collapsed, so renumbering an unrelated Grid could report success while preserving a Grid-label ambiguity that `GridNamingHealthService` classifies as `GRID_LABEL_DUPLICATE` / Error.

The batch cannot repair duplicate labels when every owner of that duplicate is outside the target set, so this path now fails closed before `ProjectState.Touch()` or target mutation. Duplicates involving target Grids remain repairable because target pre-renumber labels are intentionally excluded from the reserved-label set.

## Implemented

- Normalize each non-empty non-target Grid label with the existing trim behavior.
- Require `reservedLabels.Add(normalizedExisting)` to succeed; the second case-insensitive owner now throws `InvalidOperationException`.
- Preserve existing outside-batch planned-label collision behavior and all capacity, ID, category, sequence, affix, ordering and no-op contracts.
- Add isolated `GridNamingReservedLabelIntegritySmoke` coverage.
- Add module-initializer registration without editing the shared smoke registration hotspot.
- Add `scripts/preflight-grid-naming-reserved-label-integrity.py` and a focused implementation plan.

## Regression contract

The committed smoke proves:

1. non-target labels `"  KEEP  "` and `"keep"` reject an unrelated target renumber before mutation, with `ProjectState.ChangeVersion`, target `GridLabel` and target `GridSequenceIndex` unchanged;
2. a pre-existing duplicate shared by one target and one non-target remains repairable when the target is renumbered to a new label.

## Moving-main safety

- The claim was committed separately on `main` before implementation.
- Post-claim source was re-fetched and still contained the confirmed defect.
- Direct fast-forward integration was attempted twice while `main` was moving rapidly; both stale-parent updates were rejected by GitHub with HTTP 422 and no force update was used.
- The patch was then rebased onto moving `main` through an isolated branch and PR #600.
- Before merge, compare checks showed concurrent changes did not touch `GridNamingService.cs` or this lane's four new files.
- PR #600 changed exactly the five reserved implementation files and was squash-merged with expected head `6ce8ec1868856e91979a0c0d8ed3693ed36b0aa5`.

## Validation boundary

Source/diff review and committed CAD-independent smoke/static regression coverage are present. No executable smoke/preflight PASS is claimed from this GitHub-only environment. No GitHub Actions workflow dispatch and no BricsCAD V25 runtime PASS are claimed.

## Completion evidence

PR #600 is merged on `main` as `094edd329008ec376e3d47bf49f901447bb601c7`. Grid renumber now fails closed on duplicate reserved labels that the requested batch cannot repair, while preserving target-involved repair semantics.
