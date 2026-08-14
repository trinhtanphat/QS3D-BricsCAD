# Work claim — historical multi-agent integration audit

- Status: `ACTIVE`
- Agent: `chatgpt-20260814-historical-integration-audit-56sol`
- Registered: `2026-08-14T17:11:00+07:00`
- Baseline main SHA: `c2b4e2a49bbfa5690adc5da96b6c80a4bb30ab09`
- Priority: Owner-requested retrospective audit of prior multi-agent direct-to-main / PR integrations for lost code, semantic overwrites, stranded work, and missing regression protection.

## Reserved scope

Read-only historical/integration audit across Git commit history, merge/revert evidence, PR/branch state, current source contracts, tests and preflights. A preliminary Floor/Zone active-id regression hypothesis was investigated and disproved after tracing the later canonicalization contract: `ProjectState.ActiveFloorId` / `ActiveZoneId` now trim and version persisted changes, `ProjectFloorService.SetActive` / `ProjectZoneService.SetActive` canonicalize case aliases through those setters, and `ActiveFloorZoneCanonicalRegressionSmoke` locks single-version canonical repair plus exact canonical no-op.

A separate current defect is now confirmed in `ProjectZoneService.Update`: its local `Required(...)` validation accepts control characters, so the method can call `project.Touch()` and only then fail when `ZoneDefinition.Name` rejects the same name. The rejected update therefore mutates `ChangeVersion` / `UpdatedUtc`, violating failure atomicity. This claim reserves only the production validation boundary and one focused smoke regression needed to reject that input before mutation.

## Expected surfaces

- `docs/agent-work-claims/2026-08-14-chatgpt-56sol-historical-integration-audit.md`
- `docs/HISTORICAL-INTEGRATION-AUDIT-2026-08-14.md` after evidence is collected
- `src/QS3D.Core/Domain/ProjectZoneService.cs` — make service-level required text validation reject control characters before any project mutation
- `tests/QS3D.Core.SmokeTests/ProjectZoneUpdateFailureAtomicitySmoke.cs` — prove invalid control-character rename is rejected without changing zone/project state
- read-only Git history / PR / branch / issue / current source / tests / existing CI evidence

## Excluded scope

- every other product source/test/script/runtime file unless a later concrete defect is proven and this claim is amended again first with exact surfaces
- every source/test/script/runtime surface currently reserved by another `ACTIVE` or `BLOCKED` claim
- LOCAL_ONLY BricsCAD runtime qualification and private-machine evidence
- unrelated GitHub Actions/release changes
- arbitrary backlog feature development unrelated to a proven integration-loss finding

## Validation plan

- inspect commit topology, merge/revert/restore clusters and high-concurrency landing windows;
- distinguish stale historical contracts from the latest intentionally superseding contract before proposing any fix;
- verify the Zone rename failure-atomicity defect on current source semantics;
- add a focused deterministic smoke that captures project `ChangeVersion`, `UpdatedUtc`, and zone name, attempts a control-character rename, and asserts all captured state is unchanged after rejection;
- keep the production change limited to pre-mutation input validation in `ProjectZoneService.Required(...)`;
- refresh current `main` and active claims before implementation/landing, and do not overlap another agent's owned surfaces;
- for any additional concrete remote-safe defect, amend this claim first, then implement and add regression coverage without colliding with another claim.

## Coordination

This lane deliberately excludes current feature/runtime implementation claims. The earlier temporary Floor/Zone active-id reservation was released after that hypothesis was disproved. The new reservation is narrower: only `ProjectZoneService.cs` and the new `ProjectZoneUpdateFailureAtomicitySmoke.cs` regression. Current open-PR inspection found no competing PR before registration of this amendment; `main` remains live and must be refreshed before every material write.

## Evidence notes

- `0ce741622c31fe794aa3784ac45c304309d8c2a4` restored the older #545 trimmed/case-insensitive semantic no-op interpretation after #590.
- `2d59c7e11f156387b452e86077a23a6f0f8a8db0` later intentionally superseded that behavior with exact canonical repair plus one version increment for aliases.
- `9e65b58d40b0d0937c4de4dc7dbfbd6bbb55838b` intentionally removed the explicit service `Touch()` so the persisted active-id property setter is the single version boundary.
- `191e0509dcad66c9c5029bfd512a795d97f1486f` aligned current fixtures with `SetActiveContextId` trimming.
- current `ActiveFloorZoneCanonicalRegressionSmoke` still verifies alias repair, single versioning and exact canonical no-op; therefore that historical cluster is `SUPERSEDED / SAFE`.
- current `ProjectZoneService.Update` validates length/blank input through its own `Required(...)`, then calls `project.Touch()`, then assigns `zone.Name`; `ZoneDefinition.Name` independently rejects control characters. A control-character rename can therefore fail after the project version/timestamp has already changed. This is classified `CONFIRMED_REGRESSION` / failure-atomicity defect.

## Completion condition

A pushed historical-integration audit report records the evidence and classifications; the reserved Zone update failure-atomicity defect is fixed with deterministic regression protection and verified as reachable from refreshed `origin/main`; every other proven remote-safe defect discovered within a safely reservable surface is fixed with regression protection; any blocked/LOCAL_ONLY/actively-owned finding is handed off rather than overwritten; this claim is then marked `COMPLETED` with final commit ancestry and validation evidence.
