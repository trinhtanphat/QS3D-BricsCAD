# KeepTarget interchange indexed resolution claim

- Status: ACTIVE
- Agent: Codex `/root/audit_performance_next`
- Registered: 2026-08-15 10:51 +07:00
- Baseline `main`: `c5fbe4af9fb98383679f279e33d9b93eb2ec737d`
- Issue: #81
- Claim branch: `agent/codex/issue81-keeptarget-resolution-index-claim-20260815`
- Claim merge: `79f9be8c0ed018a93dd4eedb516b4d5f2580c930`
- Implementation baseline `main`: `87c38a532673b16f315ab766333870d4200a8677`
- Implementation branch: `agent/codex/issue81-keeptarget-resolution-index-impl-20260815`

## Defect

`ProjectInterchangeKeepTargetImporter.Import(...)` iterates every validated source Zone, Floor, Family, and Element. For each identity, `ShouldAdd(...)` runs `resolution.Items.Single(...)` over the complete resolution plan. Accepted interchange bounds allow up to 2,000 Zones, 2,000 Floors, 10,000 Families, and 100,000 Elements, so mutation selection is quadratic even though planning has already produced one bounded resolution item per source identity.

## Reserved scope

- `src/QS3D.Core/Export/ProjectInterchangeKeepTargetImporter.cs`: build one case-insensitive resolution-action index and replace the four per-identity full-plan scans with indexed lookup.
- `tests/QS3D.Core.SmokeTests/ProjectInterchangeKeepTargetImporterSmoke.cs` or one separate auto-registered focused smoke: preserve mixed-case Add/Keep behavior and resulting state.
- One focused remote-safe structural preflight under `scripts/` may pin the absence of per-identity `Single(...)` scans and the presence of the index.
- This claim record.

## Preservation and exclusions

Preserve valid-plan exception behavior, deterministic source ordering, Add/Keep decisions, plan/result counts, metadata, audit text, active-context restoration, target validation, atomic rollback, JSON/planner capacity limits, and all public interchange contracts.

Do not modify V25/V26/native adapters, UI, licensed runtime evidence, LOCAL/private data, release surfaces, workflows, GitHub Actions, BCF, the target-map Unicode lane, or any other ACTIVE/BLOCKED claim/open PR. Do not claim native or end-to-end timing improvement from remote structural evidence.

## Validation plan

- Run the focused structural preflight and focused KeepTarget smoke coverage.
- Build `QS3D.Core` and `QS3D.Core.SmokeTests` in Release with zero warnings/errors.
- Run the full Core smoke executable.
- Run the repository remote-safe preflight aggregate without dispatching or modifying GitHub Actions.
- Refresh `origin/main`, re-audit collisions, inspect the final diff, push the task branch, and open an implementation PR; stop before merge.

## Implementation branch evidence

- Focused `preflight-interchange-keeptarget-resolution-index.py`: PASS.
- `QS3D.Core` Release build: PASS, 0 warnings / 0 errors.
- `QS3D.Core.SmokeTests` Release build: PASS, 0 warnings / 0 errors.
- Full Core smoke executable: `ALL PASS`.
- Aggregate remote-safe preflight: PASS, all 817 discovered gates.
- No GitHub Actions, native/BricsCAD runtime, LOCAL/private data, release, or workflow operation was used.
