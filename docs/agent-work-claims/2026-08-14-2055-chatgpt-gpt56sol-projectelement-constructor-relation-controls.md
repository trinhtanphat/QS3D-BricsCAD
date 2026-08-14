# Work claim — ProjectElement constructor relation control-character parity

- Status: `COMPLETED`
- Agent: `chatgpt-gpt56sol-projectelement-constructor-relations`
- Registered: `2026-08-14T20:55:00+07:00`
- Scope expanded: `2026-08-14T20:58:00+07:00`
- Completed: `2026-08-14T21:08:00+07:00`
- Baseline main SHA: `bd34bc749bf1214e240de1c3b2a5ee42b52291fb`
- Claim-visible main SHA: `c29807f08d4e3d66fd98ab123ffc69a08edff4d5`
- Implementation branch: `agent/chatgpt-gpt56sol/projectelement-constructor-relation-controls`
- Implementation commit: `d9feb3dd2ab59feaf8e0c3d8f528d74001e39958`
- Integration batch: `integration/20260814-projectelement-constructor-relation-controls`
- Integration candidate: `1a1e7a64e1e94118c27b7b4ffbe9bbc1142d39fb`
- Pull request: `#1340`
- Final source landing on main: `412f3ca366473388d73944434e3235333c33d82e`
- Priority: Core P1 invariant + adjacent deterministic smoke regression found during owner-requested whole-repository review
- Task Key: `CORE-PROJECTELEMENT-CONSTRUCTOR-RELATION-CONTROLS`

## Confirmed defects

1. `ProjectElement.FamilyId`, `FloorId`, and `ZoneId` setters normalize through `NormalizeOptionalRelationId(...)`, which trims surrounding whitespace and rejects control characters. The five-argument `ProjectElement` constructor bypassed that invariant and assigned the three relation ids with direct `Trim()` calls, allowing constructor-only control-character state that the equivalent public setters rejected.
2. `ProjectElementRelationPersistabilitySmoke` retained the pre-canonical-drawing-fingerprint expectation that surrounding whitespace survived assignment even though the completed production drawing-fingerprint canonicality lane intentionally trims that identity.

## Completed implementation

- The five-argument constructor now assigns `_familyId`, `_floorId`, and `_zoneId` through the same `NormalizeOptionalRelationId(...)` helper used by the public setters.
- Focused module-initializer smoke coverage now rejects constructor control characters independently for Family/Floor/Zone, retains constructor padding/null canonicalization and setter atomicity checks, and expects the already-canonical trimmed drawing fingerprint.
- No production drawing-fingerprint behavior was changed.

## Owned surfaces

- `src/QS3D.Core/Domain/ProjectElement.cs`
- `tests/QS3D.Core.SmokeTests/ProjectElementRelationPersistabilitySmoke.cs`
- this claim file

## Concurrency / integration evidence

- The agent branch was exactly one implementation commit ahead of its post-expansion claim baseline.
- Agent diff and final integration diff were limited to the two owned source/test files.
- Concurrent QSDB negative-quantity changes through `aee493fa6e67b03551883bfbb047e743b38b6bba` touched separately owned persistence validator/smoke surfaces and were preserved.
- Integration branch was created from current `main` `45cf8b20e30947a0532006831a087893307c69ef` and the integration commit retained `d9feb3dd2ab59feaf8e0c3d8f528d74001e39958` as a parent.
- PR #1340 was mergeable and landed once into `main` at `412f3ca366473388d73944434e3235333c33d82e`.
- Post-merge read-back at exact source landing confirms the constructor uses `NormalizeOptionalRelationId(...)` for all three relation ids and the updated smoke contains the constructor control-character/null coverage plus canonical drawing-fingerprint expectation.

## Validation classification

- Remote static/diff/read-back validation: `PASS`.
- Focused deterministic smoke source: committed and module-registered; execution was `NOT_RUN` in this connector-only session because no .NET runner is exposed here.
- GitHub Actions: not manually dispatched or rerun. Any owner-approved post-integration automatic V25 cloud run is separate evidence.
- Licensed BricsCAD V25 runtime / NETLOAD / native UI: not required for this pure-Core invariant lane and no LOCAL_ONLY PASS is claimed.

## Completion

This lane is complete: the claimed implementation is reachable from `main`, the exact source landing SHA is recorded, no reserved concurrent surface was overwritten, and the claim is closed only after final-main read-back.
