# Work claim — historical multi-agent integration audit

- Status: `ACTIVE`
- Agent: `chatgpt-20260814-historical-integration-audit-56sol`
- Registered: `2026-08-14T17:11:00+07:00`
- Baseline main SHA: `c2b4e2a49bbfa5690adc5da96b6c80a4bb30ab09`
- Priority: Owner-requested retrospective audit of prior multi-agent direct-to-main / PR integrations for lost code, semantic overwrites, stranded work, and missing regression protection.

## Reserved scope

Read-only historical/integration audit across Git commit history, merge/revert evidence, PR/branch state, current source contracts, tests and preflights. A preliminary Floor/Zone regression hypothesis was investigated and disproved after tracing the later canonicalization contract: `ProjectState.ActiveFloorId` / `ActiveZoneId` now trim and version persisted changes, `ProjectFloorService.SetActive` / `ProjectZoneService.SetActive` canonicalize case aliases through those setters, and `ActiveFloorZoneCanonicalRegressionSmoke` locks single-version canonical repair plus exact canonical no-op. No Floor/Zone production or smoke patch is required by this audit.

## Expected surfaces

- `docs/agent-work-claims/2026-08-14-chatgpt-56sol-historical-integration-audit.md`
- `docs/HISTORICAL-INTEGRATION-AUDIT-2026-08-14.md` after evidence is collected
- read-only Git history / PR / branch / issue / current source / tests / existing CI evidence

## Excluded scope

- all product source/test/script/runtime files unless a later concrete defect is proven and this claim is amended again first with exact surfaces
- every source/test/script/runtime surface currently reserved by another `ACTIVE` or `BLOCKED` claim
- LOCAL_ONLY BricsCAD runtime qualification and private-machine evidence
- GitHub Actions dispatch, rerun, release or publication without separate explicit owner authorization under `CI_POLICY.md`
- arbitrary backlog feature development unrelated to a proven integration-loss finding

## Validation plan

- inspect commit topology, merge/revert/restore clusters and high-concurrency landing windows;
- check representative and suspicious diffs for semantic overwrite or accidental loss;
- distinguish stale historical contracts from the latest intentionally superseding contract before proposing any fix;
- inspect current source/tests/guards for protection of the latest contract;
- inspect current open PR/branch evidence for potentially stranded required work where repository evidence is sufficient;
- search current tree for unresolved merge markers and regression-risk patterns;
- classify findings as `SAFE`, `SUPERSEDED`, `INTENTIONAL_REVERT`, `SUSPICIOUS_LOSS`, `CONFIRMED_REGRESSION`, or `STRANDED_WORK`;
- for any concrete remote-safe defect, amend this claim first, then implement and add regression coverage without colliding with another claim.

## Coordination

This lane deliberately excludes current feature/runtime implementation claims, including the active #79 Grid V25 planner/CI lane observed before registration. The temporary Floor/Zone write reservation is explicitly released by this amendment because no patch is warranted; those files return to normal coordination ownership. Historical read-only inspection remains allowed across other areas, but no write may overlap their reserved surfaces. `main` remains live and must be refreshed before every material write.

## Evidence notes

- `0ce741622c31fe794aa3784ac45c304309d8c2a4` restored the older #545 trimmed/case-insensitive semantic no-op interpretation after #590.
- `2d59c7e11f156387b452e86077a23a6f0f8a8db0` later intentionally superseded that behavior with exact canonical repair plus one version increment for aliases.
- `9e65b58d40b0d0937c4de4dc7dbfbd6bbb55838b` intentionally removed the explicit service `Touch()` so the persisted active-id property setter is the single version boundary.
- `191e0509dcad66c9c5029bfd512a795d97f1486f` aligned current fixtures with `SetActiveContextId` trimming.
- current `ActiveFloorZoneCanonicalRegressionSmoke` still verifies alias repair, single versioning and exact canonical no-op; therefore this historical cluster is classified `SUPERSEDED / SAFE`, not a current regression.

## Completion condition

A pushed historical-integration audit report records the evidence and classifications; every proven remote-safe defect discovered within a safely reservable surface is fixed with regression protection and verified as reachable from refreshed `origin/main`; any blocked/LOCAL_ONLY/actively-owned finding is handed off rather than overwritten; this claim is then marked `COMPLETED` with final commit ancestry and validation evidence.
