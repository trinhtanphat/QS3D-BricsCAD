# Work claim — historical multi-agent integration audit

- Status: `ACTIVE`
- Agent: `chatgpt-20260814-historical-integration-audit-56sol`
- Registered: `2026-08-14T17:11:00+07:00`
- Baseline main SHA: `c2b4e2a49bbfa5690adc5da96b6c80a4bb30ab09`
- Priority: Owner-requested retrospective audit of prior multi-agent direct-to-main / PR integrations for lost code, semantic overwrites, stranded work, and missing regression protection.

## Reserved scope

Historical/integration audit across Git commit history, merge/revert evidence, PR/branch state, current source contracts, tests and preflights. The audit has now confirmed that the Floor/Zone active-id semantic no-op contract restored by `0ce741622c31fe794aa3784ac45c304309d8c2a4` was later overwritten in current `main`: both `SetActive` implementations again use exact ordinal identity. This claim therefore reserves the two production services plus their focused Core smoke files solely to restore and lock the established trimmed/case-insensitive semantic no-op behavior.

## Expected surfaces

- `docs/agent-work-claims/2026-08-14-chatgpt-56sol-historical-integration-audit.md`
- `docs/HISTORICAL-INTEGRATION-AUDIT-2026-08-14.md`
- `src/QS3D.Core/Domain/ProjectFloorService.cs` — restore established active-id semantic no-op comparison
- `src/QS3D.Core/Domain/ProjectZoneService.cs` — restore established active-id semantic no-op comparison
- `tests/QS3D.Core.SmokeTests/ProjectFloorServiceSmoke.cs` — regression guard for trimmed/case-insensitive active Floor alias semantic no-op
- `tests/QS3D.Core.SmokeTests/ProjectZoneServiceSmoke.cs` — regression guard for trimmed/case-insensitive active Zone alias semantic no-op
- read-only Git history / PR / branch / issue / existing CI evidence

## Excluded scope

- every other source/test/script/runtime surface currently reserved by another `ACTIVE` or `BLOCKED` claim
- LOCAL_ONLY BricsCAD runtime qualification and private-machine evidence
- GitHub Actions dispatch, rerun, release or publication without separate explicit owner authorization under `CI_POLICY.md`
- arbitrary backlog feature development unrelated to a proven integration-loss finding
- any additional source/test/script edit before a claim-only amendment reserves its exact surface

## Validation plan

- inspect commit topology, merge/revert/restore clusters and high-concurrency landing windows;
- check representative and suspicious diffs for semantic overwrite or accidental loss;
- restore the proven Floor/Zone semantic no-op comparison exactly to the established trimmed/case-insensitive contract without unrelated service changes;
- add focused regression assertions proving semantically equivalent trimmed/case-insensitive Floor/Zone active-id aliases are not rewritten;
- inspect current open PR/branch evidence for potentially stranded required work where repository evidence is sufficient;
- search current tree for unresolved merge markers and regression-risk patterns;
- classify findings as `SAFE`, `SUPERSEDED`, `INTENTIONAL_REVERT`, `SUSPICIOUS_LOSS`, `CONFIRMED_REGRESSION`, or `STRANDED_WORK`;
- for any further concrete remote-safe defect, amend this claim first, then implement and add regression coverage without colliding with another claim.

## Coordination

This lane deliberately excludes current feature/runtime implementation claims, including the active #79 Grid V25 planner/CI lane observed before registration. Exact claim-directory checks immediately before this amendment found no reservation naming `ProjectFloorService.cs` or `ProjectZoneService.cs`; earlier exact checks also found no competing reservation for the two smoke files. Historical read-only inspection is allowed across other areas, but no write may overlap their reserved surfaces. `main` remains live and must be refreshed before every material write.

## Completion condition

A pushed historical-integration audit report records the evidence and classifications; the confirmed Floor/Zone production regression is restored and guarded in the reserved Core smoke files; every other proven remote-safe defect discovered within a safely reservable surface is fixed with regression protection and verified as reachable from refreshed `origin/main`; any blocked/LOCAL_ONLY/actively-owned finding is handed off rather than overwritten; this claim is then marked `COMPLETED` with final commit ancestry and validation evidence.
