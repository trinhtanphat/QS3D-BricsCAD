# Work claim — HostWallId health canonicality

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-hostwall-health-canonicality`
- Registered: `2026-08-12T09:33:00+07:00`
- Completed: `2026-08-12T09:42:00+07:00`
- Baseline main SHA: `bb786801c32ff64e8e89fec09c32ee1376e8e640`
- Priority: P1 — baseline Model Health must surface non-canonical persisted HostWallId aliases before HostLink mutation repair.
- Task Key: `CORE-MODEL-HEALTH-HOSTWALL-CANONICALITY`

## Confirmed defect

`HostLinkService.LinkOpening(...)` writes exact `wall.Id` to `HostWallId` and explicitly treats the property as canonical only when the raw value matches `wall.Id` with `StringComparison.Ordinal`; same-host aliases are repaired by the mutation path. Baseline `ModelHealthService.ValidateHost(...)` normalized the stored property and resolved case-insensitively, so padded/case-varied aliases could look healthy until a HostLink mutation repaired them.

## Implemented

- Claim: `fae9fc912e18eb4ebb4aeaed2b393a4b998316bb`
- Branch source: `5ed0edbd6f2edbb01a8bcc10e53bddb4c8e0e0fe`
- Branch smoke / reviewed PR head: `5fa7b1fb848e69a9d10008e8b65e486bc6d850fe`
- PR: `#699`
- Squash merge on `main`: `6788dcb9c6991b323aa987e547f25e99e6bfea09`

`ValidateHost(...)` now preserves existing missing/ambiguous/invalid-target precedence, then emits `HOST_REFERENCE_NON_CANONICAL` when the raw persisted HostWallId differs from the exact resolved semantic host identity. Existing wrong-category diagnostics remain intact.

## Regression coverage

`ModelHealthHostWallCanonicalitySmoke` covers padded and case-varied aliases, exact canonical control, missing target, duplicate ambiguity, and canonical wrong-category behavior.

## Validation

- Read back current `ModelHealthService.cs` and focused smoke from merged `main`.
- Compared squash merge `6788dcb9c6991b323aa987e547f25e99e6bfea09` to later `main` `d2c24e40d3ecfd9c214a28740f8ce22b3a2bc2f1`: status `ahead`, `ahead_by=6`, `behind_by=0`, merge base exactly the squash commit; later changes were unrelated.
- No GitHub Actions workflow was dispatched. No full .NET build or licensed BricsCAD V25/V26 runtime PASS is claimed from this remote lane.
