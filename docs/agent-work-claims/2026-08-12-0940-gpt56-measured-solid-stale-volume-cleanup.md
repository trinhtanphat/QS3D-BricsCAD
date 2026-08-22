# Work claim — Measured solid stale volume cleanup

- Status: `COMPLETED`
- Agent: `gpt-5.6-sol-chatgpt`
- Registered: `2026-08-12T09:40:30+07:00`
- Completed: `2026-08-12T09:55:00+07:00`
- Baseline main SHA: `9837809e6c06b86b0c89d10f630441954dbd7bec`
- Pull Request: `#724`
- Reviewed head: `84167dcf2762f79768532dd05fbdbb3908125382`
- Merge SHA: `1dda71a4da95c265217c37f7499e387691025842`
- Priority: owner-requested continue-all source-safe bug fixing

## Confirmed defect

`MeasuredSolidQuantityPolicy.Apply()` wrote `MeasuredSolidVolumeM3`, `GrossVolumeM3`, and `NetVolumeM3` together when measured volume was applicable, but a later apply without applicable measured volume removed only `MeasuredSolidVolumeM3`, leaving stale policy-derived gross/net quantities.

## Completed implementation

- When previously applied measured volume becomes unavailable or unsupported, remove `MeasuredSolidVolumeM3`.
- Remove `GrossVolumeM3` / `NetVolumeM3` only when they still match the previously measured value, preserving independently changed overrides.
- Preserve measured surface-area behavior and ordinary successful measured-volume application.
- Focused Core smoke covers source removal, independent override preservation and category invalidation; smoke is registered in the existing harness.

## Evidence

- PR #724 exact patch reviewed.
- Moving-main comparison showed no overlap with `MeasuredSolidQuantityPolicy.cs` or its smoke/registration before merge.
- Squash merge: `1dda71a4da95c265217c37f7499e387691025842`.

## Validation boundary

No GitHub Actions were dispatched. No local/full build or licensed BricsCAD V25/V26 runtime PASS is claimed.
