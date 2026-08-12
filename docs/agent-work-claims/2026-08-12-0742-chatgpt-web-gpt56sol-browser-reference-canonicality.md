# Work claim — Project Browser reference canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-browser-reference-canonicality-20260812-0742`
- Registered: `2026-08-12T07:42:00+07:00`
- Baseline main SHA: `53d6a8e3148c33ba3c9f719799dd77df9d6dd51a`
- Priority: P2 — keep Browser grouping from silently normalizing semantic relation state that QSDB persistence rejects.

## Confirmed defect

`ProjectElement.FloorId` and `ZoneId` are mutable. QSDB persistence rejects non-empty relation IDs with surrounding whitespace, while `ProjectBrowserPlanner.ValidateReferences(...)` previously trimmed both IDs before lookup. A directly mutated `" F1 "` / `" Z1 "` relation could therefore be displayed as valid Browser structure even though the same project could not be persisted canonically.

## Implemented fix

- Browser reference validation now fails closed when a non-empty `FloorId` or `ZoneId` is whitespace-only or contains leading/trailing whitespace.
- Empty/unassigned relations remain valid.
- Canonical relation lookup remains case-insensitive; smoke coverage explicitly accepts `f1`/`z1` against `F1`/`Z1` definitions.
- Grouping/order/count semantics, `MaxElements`, `MaxReferenceDefinitions`, virtualization `MaxNodes`, and unrelated workspace/query behavior remain unchanged.

## Reserved surfaces

- `src/QS3D.Core/Navigation/ProjectBrowserPlanner.cs`
- `tests/QS3D.Core.SmokeTests/ProjectBrowserReferenceCanonicalitySmoke.cs`
- this claim file

## Integration evidence

- Claim registration: `976fa870d3ed2e361b184e07386dd06d5f25f1c9`.
- Branch source commit: `1ba3299a59d82988e552deae9d589c31925762ab`.
- Branch smoke commit: `7cb99eefb0fda1dc1c2a626459034b9b6a014037`.
- Branch diff was exactly two reserved files: source + new focused smoke.
- Comparison from claim registration to then-current `main` `8e24829a0eed1938bc8537043a1ec248db0089ca` showed 27 intervening commits and no modification of `ProjectBrowserPlanner.cs` or the new smoke path.
- Safe CAS source integration on current `main`: `df83b94c961059e0af67e058efe72220ebe52bf1`.
- Safe CAS smoke integration on current `main`: `125a92e1603afb68a02426464f226c24de944ad8`.
- PR `#625` subsequently completed at `e6f99dbefbbdba7bd7cf064a0d11ffd49aea682b`; its substantive patch was already present through the two CAS commits, so the PR completion added no additional source/test delta.
- Post-integration re-read confirmed source blob `f2238875a2941023bd8a5f1ebac9452c7cf42aa4` and smoke blob `35e3e471db447c3bfec3bf03ff8a654c71e87093` on `main`.

## Explicit coordination

The concurrent Browser workspace container-order claim owned `ProjectBrowserWorkspaceStateStore.cs`; this lane did not touch that file or its smoke surfaces.

## Validation boundary

Committed deterministic Core smoke coverage plus exact source/diff review. No GitHub Actions were dispatched and no BricsCAD V25 runtime PASS is claimed.
