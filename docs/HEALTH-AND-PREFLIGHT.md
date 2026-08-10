# QS3D health and preflight contracts

## Full model health

`QS3DHEALTHALL` is the broad review entry point. It aggregates and deduplicates:

- `ModelHealthService` for semantic/project/source/generated-host integrity;
- `GeneratedGeometryStaleHealthService` for generated-output freshness;
- `GeneratedRebarHealthService` for longitudinal and BBS-shape generated solids;
- `GeneratedTieRebarHealthService` for column ties;
- `GeneratedBeamStirrupHealthService` for beam stirrups.

The command resolves live CAD handles before health inspection and opens the existing `ModelHealthWindow`. Locate actions prefer the generated handle family associated with the issue code, then fall back to semantic source handles.

Specialized health commands remain useful for diagnosis, but product UI should prefer `QS3DHEALTHALL` when the user asks for a complete model check.

## Generated freshness is not the same as Dirty

Do not infer stale CAD geometry from `element.Dirty != None`.

- Geometry/Properties/Relations edits mark generated output snapshots stale.
- Quantity-only dirty state does not.
- Replacing/removing the snapshotted handle set resolves stale state for that output family.

Health code must call the `ProjectElement.IsGenerated...Stale()` APIs.

## UI command wiring

`scripts/preflight-command-wiring.py` collects QS3D `CommandMethod` registrations and checks command references from:

- XAML `Tag="QS3D..."` buttons;
- `RibbonButtonSpec` definitions;
- simple UI command-dispatch calls.

Every UI/Ribbon command reference must resolve to exactly one registered command. This prevents multi-agent rename races from creating buttons that only fail at BricsCAD runtime with `Unknown command`.

## Aggregate feature preflight

`scripts/preflight.py` remains the generic repository/source policy guard.

`scripts/preflight-all.py` discovers every `scripts/preflight-*.py` feature gate except itself and runs each in deterministic filename order. It has a per-gate timeout and reports every failed gate before exiting nonzero.

Both GitHub Actions workflows remain `workflow_dispatch` only. Their source-level preflight contract is:

1. run `scripts/preflight.py`;
2. run `scripts/preflight-all.py`;
3. continue to Core build/smoke tests and, for the V25 integration workflow, the licensed BricsCAD build/runtime stages.

Adding a new `preflight-<feature>.py` therefore automatically includes that gate in future manually dispatched CI without editing both workflow YAML files again.

## What these gates do not prove

Static preflight can prove source wiring, guard presence and regression registration. Core smoke tests can prove deterministic code that does not require BricsCAD. Neither proves:

- V25 `BrxMgd.dll` / `TD_Mgd.dll` compile compatibility for the newest head;
- licensed `NETLOAD`/DemandLoad behavior;
- BIM command availability under a specific BricsCAD edition/license;
- native `Solid3d` boolean robustness on private DWGs;
- visual/DPI/Ribbon parity on a real workstation.

Those remain runtime gates and must not be reported as passed without actual evidence.
