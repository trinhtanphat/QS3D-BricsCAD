# Work claim — Active Floor/Zone canonicalization regression restoration

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-active-floor-zone-canonical-regression-20260812-0936`
- Registered: `2026-08-12T09:36:00+07:00`
- Completed: `2026-08-12T09:43:00+07:00`
- Baseline main SHA: `97437f21003d2011bcb332e68708668739383cf7`
- Claim registration SHA: `fc365b54f7d5db899b48ffd5dbd2b195253cd911`
- Pull Request: `#706`
- Implementation merge SHA: `2d59c7e11f156387b452e86077a23a6f0f8a8db0`
- Priority: owner-requested continue-all regression repair

## Confirmed regression

Commit `3fa9a709307fbd9e9f1614f6b072efd2affe449f` previously established that `ProjectFloorService.SetActive(...)` and `ProjectZoneService.SetActive(...)` must repair canonical-equivalent aliases (case/outer whitespace) to the exact project-owned ID, while an already-exact active ID remains a no-op. Current `main` had regressed both methods to `Trim()` + case-insensitive no-op checks, and the focused canonicalization cases added by that commit were no longer present in the current Floor/Zone service smoke files.

Consequently, persisted/publicly-mutated state such as `ActiveFloorId = " FLOOR-A "` or `ActiveZoneId = " ZONE-A "` could survive a successful SetActive call instead of being repaired to the exact owned identifier, despite model-health checks treating non-canonical active Floor/Zone IDs as integrity issues.

## Completed restoration

- Restored exact ordinal active-ID no-op checks in both `ProjectFloorService.SetActive(...)` and `ProjectZoneService.SetActive(...)`.
- Canonical-equivalent aliases are now rewritten to the exact project-owned ID and touch the project exactly once.
- Exact canonical active IDs remain true no-ops.
- Missing target IDs still fail before mutation.
- Assignment, delete, vertical placement and reference semantics were left unchanged.
- Added isolated auto-registered `ActiveFloorZoneCanonicalRegressionSmoke` covering Floor and Zone alias repair, exact canonical no-op, and missing-ID non-mutation.

## Integration evidence

- Prior contract commit: `3fa9a709307fbd9e9f1614f6b072efd2affe449f`.
- Claim registration: `fc365b54f7d5db899b48ffd5dbd2b195253cd911`.
- PR `#706` diff contained exactly two source-line changes plus the focused smoke file and was mergeable after synchronizing with moving `main`.
- PR `#706` squash-merged to `main` as `2d59c7e11f156387b452e86077a23a6f0f8a8db0`.
- Immediate main readback confirmed Floor source blob `733753aa4c881ef4e3afa33a37e13c26f49b8097`, Zone source blob `851880f56385848d233ef71eb12a5bc11631d64e`, and smoke blob `64a9e8aba1dee8f2fc1de7ea2c09b50d3f3c50f4`.

## Validation boundary

Deterministic source/smoke implementation plus GitHub diff/readback only. No GitHub Actions/full .NET build/release dispatch and no licensed BricsCAD V25/V26 runtime PASS was executed or claimed in this lane.
