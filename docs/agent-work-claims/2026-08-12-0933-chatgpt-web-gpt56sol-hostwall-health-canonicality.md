# Work claim — HostWallId health canonicality

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-hostwall-health-canonicality`
- Registered: `2026-08-12T09:33:00+07:00`
- Baseline main SHA: `bb786801c32ff64e8e89fec09c32ee1376e8e640`
- Priority: P1 — baseline Model Health must surface non-canonical persisted HostWallId aliases before HostLink mutation repair.
- Task Key: `CORE-MODEL-HEALTH-HOSTWALL-CANONICALITY`

## Confirmed defect

`HostLinkService.LinkOpening(...)` writes exact `wall.Id` to `HostWallId` and explicitly treats the property as canonical only when the raw value matches `wall.Id` with `StringComparison.Ordinal`; same-host aliases are repaired by the mutation path. `ModelHealthService.ValidateHost(...)` currently trims the stored property and resolves case-insensitively, so padded/case-varied aliases can look healthy until a HostLink mutation repairs them.

## Non-overlap check

Recent HostLink global-identity work guards duplicate project element ids; physical opening host-reference canonicality covers physical-cut ownership state. Neither changes baseline `ModelHealthService.ValidateHost(...)`. No dedicated HostWallId baseline-health canonicality claim/commit was found.

## Reserved scope

- `src/QS3D.Core/Diagnostics/ModelHealthService.cs`
- one focused Core smoke regression for HostWallId health canonicality
- this claim file

Do not modify `HostLinkService`, physical opening cut state, dependencies, UI, persistence format or BricsCAD runtime code.

## Intended contract

- If a unique wall target exists but raw `HostWallId` differs from exact `wall.Id` by case and/or surrounding whitespace, health emits a dedicated `HealthSeverity.Error` canonicality diagnostic.
- Missing host keeps `MISSING_HOST`/`INVALID_HOST` behavior.
- Duplicate host identity keeps `AMBIGUOUS_HOST` without choosing an arbitrary canonical target.
- Canonical wrong-category targets keep `INVALID_HOST_CATEGORY` without a canonicality error.
- Exact canonical HostWallId preserves current behavior.

## Completion condition

HostWallId aliases are fail-visible, focused smoke coverage pins padded/case aliases plus canonical/missing/ambiguous/wrong-category controls, source + smoke are read back from merged `main`, ancestry is verified, and this claim is closed with exact commit SHAs.
