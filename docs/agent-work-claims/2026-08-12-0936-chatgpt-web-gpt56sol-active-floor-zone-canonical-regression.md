# Work claim — Active Floor/Zone canonicalization regression restoration

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-active-floor-zone-canonical-regression-20260812-0936`
- Registered: `2026-08-12T09:36:00+07:00`
- Baseline main SHA: `97437f21003d2011bcb332e68708668739383cf7`
- Priority: owner-requested continue-all regression repair

## Confirmed regression

Commit `3fa9a709307fbd9e9f1614f6b072efd2affe449f` previously established that `ProjectFloorService.SetActive(...)` and `ProjectZoneService.SetActive(...)` must repair canonical-equivalent aliases (case/outer whitespace) to the exact project-owned ID, while an already-exact active ID remains a no-op. Current `main` has regressed both methods to `Trim()` + case-insensitive no-op checks, and the focused canonicalization cases added by that commit are no longer present in the current Floor/Zone service smoke files.

Consequently, persisted/publicly-mutated state such as `ActiveFloorId = " FLOOR-A "` or `ActiveZoneId = " ZONE-A "` can survive a successful SetActive call instead of being repaired to the exact owned identifier, despite current model-health checks treating non-canonical active Floor/Zone IDs as integrity issues.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectFloorService.cs`
- `src/QS3D.Core/Domain/ProjectZoneService.cs`
- one focused auto-registered Core smoke for regression coverage
- this claim file

## Intended restoration

- Restore exact ordinal active-ID no-op checks so canonical-equivalent aliases are rewritten to `floor.Id` / `zone.Id` and touch the project exactly once.
- Preserve exact canonical no-op behavior.
- Preserve missing-ID rejection and all assignment/delete semantics.
- Add isolated smoke coverage so later stale merges cannot silently reintroduce the regression.

## Evidence

- Prior contract commit: `3fa9a709307fbd9e9f1614f6b072efd2affe449f`.
- Current `main` before claim: `97437f21003d2011bcb332e68708668739383cf7`.
- Current source readback showed both services again using trimmed case-insensitive no-op checks.
- Current `ProjectZoneServiceSmoke.cs` readback no longer contained the prior `ActiveIdCanonicalizationRepairsAliases` regression case.

## Validation boundary

Deterministic source/smoke implementation and GitHub diff/readback only. No GitHub Actions/full .NET build/release dispatch and no licensed BricsCAD V25/V26 runtime PASS will be claimed unless actually executed.
