# Work claim — Active Floor/Zone health canonicality

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-active-floor-zone-health-canonicality`
- Registered: `2026-08-12T09:22:00+07:00`
- Completed: `2026-08-12T09:30:00+07:00`
- Baseline main SHA: `cb55b30fd16e4d613ac5a105badb99376a149884`
- Priority: P1 — baseline Model Health must surface non-canonical persisted active Floor/Zone aliases before mutation repair.
- Task Key: `CORE-MODEL-HEALTH-ACTIVE-FLOOR-ZONE-CANONICALITY`

## Confirmed defect

`ProjectFloorService.SetActive(...)` and `ProjectZoneService.SetActive(...)` were hardened in `3fa9a709307fbd9e9f1614f6b072efd2affe449f` to repair any case/whitespace alias to the exact project-owned `Floor/Zone.Id`. Baseline `ModelHealthService.ValidateActiveFloor(...)` and `ValidateActiveZone(...)` still normalized persisted active ids, allowing malformed aliases to look healthy until a mutation path repaired them.

## Implemented

- Claim commit: `ee4375ad4ae975a273915947655bc16ee22af959`
- Corrected branch source commit: `1128198eec805b24ed2e3df9f2b634a032d45fce`
- Branch smoke commit / reviewed PR head: `33b375e5e9a83dfdfb8d2184df869729bad7f243`
- PR: `#694`
- Squash merge on `main`: `a73328ec252bea279b088e6f9e42d50fb6e0c0e1`

`ModelHealthService` now resolves a unique active Floor/Zone target, preserves missing and ambiguous diagnostics, and emits `ACTIVE_FLOOR_NON_CANONICAL` / `ACTIVE_ZONE_NON_CANONICAL` when the raw persisted active id differs from the exact project-owned identity.

## Regression coverage

`ModelHealthActiveFloorZoneCanonicalitySmoke` covers padded Floor aliases, case-varied Zone aliases, exact canonical controls, missing references, and duplicate-target ambiguity precedence.

## Validation

- Reviewed PR #694 net diff: only the two active validators and the focused smoke; the temporary branch-only method-signature mistake was absent from the PR.
- Read back current `ModelHealthService.cs` and the focused smoke from merged `main`.
- Compared squash merge `a73328ec252bea279b088e6f9e42d50fb6e0c0e1` to later `main` `4ea70225d91dbc07edfa256c8e29884156f2f932`: status `ahead`, `ahead_by=4`, `behind_by=0`, merge base exactly the squash commit; later changes were unrelated.
- No GitHub Actions workflow was dispatched. No full .NET build or licensed BricsCAD V25/V26 runtime PASS is claimed from this remote lane.
