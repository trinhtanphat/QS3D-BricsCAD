# QS3D health and preflight contracts

## Full model health

`QS3DHEALTHALL` is the broad review entry point. The exact service list evolves with source, but the current full-health direction covers semantic/project/source integrity, dependency health, generated-host/stale state, generated ownership, Room-finish identity, Curtain output and the current generated rebar families.

Specialized health commands remain useful for diagnosis, but product UI should prefer `QS3DHEALTHALL` when the user asks for a complete model check. `QS3DRELEASECHECK` adds stricter release-readiness/liveness/BOM/runtime-facing guards and must not be weakened merely to make incomplete project data appear green.

The command resolves live CAD handles before health inspection and opens the existing `ModelHealthWindow`. Locate actions prefer the generated handle family associated with the issue code, then fall back to semantic source handles.

## Generated freshness is not the same as Dirty

Do not infer stale CAD geometry from `element.Dirty != None`.

- Geometry/Properties/Relations edits mark generated output snapshots stale.
- Quantity-only dirty state does not.
- Replacing/removing the snapshotted handle set resolves stale state for that output family.

Health code must call the `ProjectElement.IsGenerated...Stale()` APIs and use the shared generated-owner contract rather than maintaining feature-local owner lists.

## UI command wiring

`scripts/preflight-command-wiring.py` collects QS3D `CommandMethod` registrations and checks command references from:

- XAML `Tag="QS3D..."` buttons;
- `RibbonButtonSpec` definitions;
- simple UI command-dispatch calls.

Every UI/Ribbon command reference must resolve to exactly one registered command. This prevents multi-agent rename races from creating buttons that only fail at BricsCAD runtime with `Unknown command`.

## Product boundary and Direct Draw guards

Two cross-cutting guards are especially important after the current product/authoring decisions:

- `scripts/preflight-product-boundary.py` keeps QS3D explicitly scoped as a **BricsCAD V25 plugin**, verifies the adapter remains `OutputType=Library` with a BricsCAD extension entry point, and prevents BLT/Direct-Draw wording from silently redefining the product as a standalone EXE.
- `scripts/preflight-direct-draw.py` protects P0/P1 command uniqueness, BricsCAD Ribbon/Domain Hub discoverability, Model-Space/unit-aware authoring, semantic/build ordering, generated ownership and rollback markers. P1 must reuse canonical `QS3DBUILD3D` behavior rather than forking another native builder path.

Direct Draw source/static guards still do **not** prove actual interactive editor behavior, cancellation, jigs, native Solid3d robustness or rollback on a licensed V25 workstation.

## Aggregate feature preflight

`scripts/preflight.py` remains the generic repository/source policy guard.

`scripts/preflight-all.py` discovers every `scripts/preflight-*.py` feature gate except itself and runs each in deterministic filename order. It has a per-gate timeout and reports every failed gate before exiting nonzero.

All GitHub Actions workflows remain `workflow_dispatch` only. A manually approved validation workflow should run the generic/source guards before the relevant Core/V25 build, smoke or runtime stages. Adding a new `preflight-<feature>.py` automatically includes that gate through `scripts/preflight-all.py` without requiring a new automatic trigger.

A commit, push, documentation update, `continue all`, review or handoff does **not** authorize a workflow dispatch. Follow `CI_POLICY.md`.

## What these gates do not prove

Static preflight can prove source wiring, guard presence and regression registration. Core smoke tests can prove deterministic code that does not require BricsCAD. Neither proves:

- V25 `BrxMgd.dll` / `TD_Mgd.dll` compile compatibility for the newest head unless that exact SHA was actually built;
- licensed `NETLOAD`/DemandLoad behavior;
- BIM command availability under a specific BricsCAD edition/license;
- native `Solid3d` boolean/authoring robustness on representative private DWGs;
- Direct Draw cancellation/rollback/editor interaction on the real host;
- visual/DPI/Ribbon parity on a real workstation.

Those remain runtime gates and must not be reported as passed without actual evidence.
