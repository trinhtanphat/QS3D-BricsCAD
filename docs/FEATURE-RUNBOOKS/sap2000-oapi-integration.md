# SAP2000 OAPI integration — QS3D BricsCAD

Issue: #6453

## Boundary

This feature is an interoperability bridge, not a structural-design certification layer. QS3D transfers selected CAD analytical geometry to a user-controlled SAP2000 model through CSI's OAPI. SAP2000 remains authoritative for analysis settings, code checks, section/material definitions, load cases/combinations and numerical results.

The integration is intentionally late-bound through the registered COM/OAPI ProgIDs. The repository does **not** reference, redistribute or commit `SAP2000v1.dll` or other proprietary CSI binaries.

V26 links the shared V25 host source, so the same SAP command implementation is compiled into both BricsCAD host targets. Native behavior must still be qualified separately on the exact licensed BricsCAD/SAP2000 versions in use.

## Prerequisites

- Windows x64.
- BricsCAD V25 or V26 with the matching QS3D plugin loaded.
- A locally installed SAP2000 release exposing the `SAP2000v1.Helper` / `CSI.SAP2000.API.SapObject` OAPI registration.
- Drawing units resolvable by QS3D's canonical unit policy (`INSUNITS` or an intentional QS3D project unit override). Unresolved units are rejected before OAPI mutation.
- For export into an existing SAP model, configure any project-specific material/section/area properties in SAP2000 as required. The MVP requests the OAPI `Default` property when creating objects.

## Commands

| Command | Purpose | Mutates SAP model? |
| --- | --- | --- |
| `QS3DSAPCONNECT` | Attach to a running SAP2000 instance; if none is available, start one through OAPI | No model reset |
| `QS3DSAPNEW` | Explicitly initialize a blank SAP2000 model and set present units to kN-m-C | **Yes; destructive to the current SAP model and therefore requires Yes/No confirmation** |
| `QS3DSAPEXPORT` | Export selected BricsCAD analytical geometry into the current SAP model | Yes; adds objects only |
| `QS3DSAPSAVE` | Save the current SAP model to a user-selected `.sdb` path | Saves current model |
| `QS3DSAPRUN` | Invoke SAP2000 `Analyze.RunAnalysis()` | Runs analysis |
| `QS3DSAPRESET` | Release QS3D's COM references so the next command reconnects | No; does not call `ApplicationExit` |

## Geometry mapping in this MVP

`QS3DSAPEXPORT` first builds a detached export plan from the user's selection, then performs OAPI mutations.

- `LINE` -> one SAP2000 Frame object.
- Open 2D `POLYLINE` -> one Frame object for every consecutive segment.
- Closed 2D `POLYLINE` with at least three vertices -> one SAP2000 Area object.
- Other entity types are skipped with command-line warnings.
- Coordinates are converted through the canonical `CadUnitService` from the resolved QS3D drawing unit to metres before crossing the OAPI boundary.
- Stable user names use the source CAD handle, for example `QS3D_F_<handle>` and `QS3D_A_<handle>`.
- Export never calls `InitializeNewModel` or `NewBlank`; wiping/reinitializing a model is isolated to `QS3DSAPNEW`.

The MVP does not infer analytical centre-lines from arbitrary solids, does not automatically create SAP material/section/load definitions, and does not yet import force/displacement results back into QS3D.

## Recommended workflow

1. Open the target DWG and verify QS3D's resolved drawing unit (`INSUNITS` or an intentional project override).
2. Open/configure the intended SAP2000 model, or run `QS3DSAPNEW` only when a blank model is genuinely intended.
3. Run `QS3DSAPCONNECT`.
4. Run `QS3DSAPEXPORT` and select analytical `LINE` / 2D `POLYLINE` geometry.
5. Review object connectivity and assignments in SAP2000.
6. Define/verify sections, materials, releases, diaphragms, load patterns/cases/combinations and meshing in SAP2000.
7. Run `QS3DSAPSAVE` before analysis when the SAP model has no file path yet.
8. Run `QS3DSAPRUN`.
9. Use `QS3DSAPRESET` if SAP2000 was restarted or the OAPI session became stale.

## Failure behavior

- Missing OAPI registration fails with an actionable connection error; QS3D does not fall back to shell automation.
- Unsupported/undefined drawing units fail before OAPI model mutation.
- Unsupported CAD entities are skipped; supported objects continue independently.
- A non-zero CSI return code is treated as failure and reported against the relevant operation.
- QS3D never calls SAP2000 `ApplicationExit`; a SAP process opened by the user remains user-owned.
- Per-object export errors are counted and surfaced instead of being silently treated as successful export.

## Source / CI evidence

Run:

```bash
python scripts/preflight-sap2000-integration.py
python scripts/preflight.py
python scripts/preflight-all.py
```

The focused guard verifies command registration, late-bound OAPI ProgIDs/methods, canonical unit-policy geometry mapping, explicit confirmation before blank-model initialization, V25/V26 source sharing and absence of a proprietary SAP2000 compile-time reference.

Source/static checks and host compilation are **not** licensed native SAP2000 runtime proof. A production qualification should record the exact QS3D SHA, BricsCAD major/build, SAP2000 version, OAPI registration, DWG fixture, SDB result and observed return codes.

## Follow-up product scope

A later reservation should build on this bridge rather than duplicate it. Candidate increments are semantic QS3D member mapping (beam/column/slab/wall), section/material mapping, supports/releases/diaphragms, load patterns/combinations, two-way identity mapping, analysis-result import (`P/V2/V3/T/M2/M3`, shell forces, reactions, modal/displacement results), diagram/contour visualization in BricsCAD and stale-result/freshness tracking.
