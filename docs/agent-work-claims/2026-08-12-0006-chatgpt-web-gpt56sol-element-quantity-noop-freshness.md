# Work claim — ProjectElement quantity no-op freshness

- Status: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-element-quantity-noop-freshness`
- Registered: `2026-08-12T00:06:00+07:00`
- Baseline main SHA: `9f4f28d5ed79d3b898c70078eeaeeb345b4fd9ea`
- Claim commit: `fc96f4a59e0718defafe6fbc956a22de90c09fd8`
- Implementation commit: `b78b069f3df92de6a7a740ac468edecc94216ae2`
- Regression commit: `6b3adf1355f4d361b2689f07fab193de24968bc8`
- Priority: concrete CAD-independent semantic freshness defect found during owner-requested continue-all audit

## Completed

`ProjectElement.SetQuantity(name, value)` now canonicalizes the quantity key once and returns without mutation when the existing case-insensitive key already stores the exact same finite `double` value. New or changed finite values retain the prior dictionary write and `UpdatedUtc` advancement behavior. NaN/Infinity rejection remains unchanged and occurs before mutation.

## Reserved scope delivered

- `src/QS3D.Core/Domain/ProjectElement.cs`: `SetQuantity` only.
- `tests/QS3D.Core.SmokeTests/ProjectElementQuantityNoOpSmoke.cs`: module-initialized deterministic regression coverage.
- No shared smoke-registration file changed.

## Validation actually performed

- Verified claim commit remained an ancestor of moving `main` before implementation; intervening concurrent changes were on unrelated Template/Interchange/SelectionState surfaces.
- Inspected exact implementation commit diff: the only source logic change is canonical key reuse plus exact same-value early return in `SetQuantity`; no category/dirty/generated/regeneration policy changed.
- Re-fetched current `main` source and confirmed the intended `SetQuantity` implementation is present.
- Re-fetched the new smoke file from current `main` and reviewed cases for new-value timestamp advancement, case-insensitive same-value no-op, changed-value timestamp advancement, and NaN/Infinity non-mutation.
- GitHub Actions were not dispatched and no BricsCAD V25 runtime qualification is claimed.

## Excluded scope retained

- No `ProjectElement.Category`, `MarkDirty`, `MarkClean`, generated stale, relation or property mutation policy changes.
- No quantity tolerance/rounding policy.
- No regeneration algorithm, rule-engine, ProjectState ChangeVersion, persistence schema, V25/native or UI changes.

## Completion condition

Satisfied on current `main`: exact same-value quantity assignments are side-effect free, changed/invalid-value behavior is preserved, focused regression coverage is present, and the lane is released for future work.
