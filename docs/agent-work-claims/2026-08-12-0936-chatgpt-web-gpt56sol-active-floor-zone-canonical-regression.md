# Work claim — Active Floor/Zone canonicalization regression restoration

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-active-floor-zone-canonical-regression-20260812-0936`
- Registered: `2026-08-12T09:36:00+07:00`
- Completed: `2026-08-12T09:43:00+07:00`
- Baseline main SHA: `97437f21003d2011bcb332e68708668739383cf7`
- Pull Request: `#706`
- Reviewed head: `7f269062732262bfc76249777fd106926dd3e486`
- Merge SHA: `2d59c7e11f156387b452e86077a23a6f0f8a8db0`
- Priority: owner-requested continue-all regression repair

## Confirmed regression

Commit `3fa9a709307fbd9e9f1614f6b072efd2affe449f` previously established that `ProjectFloorService.SetActive(...)` and `ProjectZoneService.SetActive(...)` must repair canonical-equivalent aliases (case/outer whitespace) to the exact project-owned ID, while an already-exact active ID remains a no-op. The pre-fix `main` had regressed both methods to `Trim()` + case-insensitive no-op checks, allowing aliases to survive a successful SetActive call.

## Completed restoration

- Restored exact ordinal active-ID no-op checks in both Floor and Zone services.
- Case/outer-whitespace aliases are rewritten to the exact project-owned ID and touch the project once.
- Exact canonical IDs remain true no-ops.
- Missing-ID rejection and existing assignment/delete semantics remain unchanged.
- Added focused auto-registered Core smoke coverage for alias repair, canonical no-op and missing-ID non-mutation.

## Evidence

- Prior contract commit: `3fa9a709307fbd9e9f1614f6b072efd2affe449f`.
- PR `#706` reviewed head: `7f269062732262bfc76249777fd106926dd3e486`.
- Moving-main comparison before merge showed no overlap with `ProjectFloorService.cs`, `ProjectZoneService.cs` or the regression smoke.
- Squash merge: `2d59c7e11f156387b452e86077a23a6f0f8a8db0`.

## Validation boundary

No GitHub Actions/full .NET build/release dispatch and no licensed BricsCAD V25/V26 runtime PASS is claimed.
