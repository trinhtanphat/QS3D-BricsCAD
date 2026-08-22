# QS3D architecture — BricsCAD V25

## Hosted-plugin boundary

QS3D is a **BricsCAD V25 hosted plugin**. `QS3D.BricsCAD.V25` is a .NET Framework library loaded into the BricsCAD process through DemandLoad or `NETLOAD`; it is **not a standalone CAD executable** and does not own an independent DWG engine or viewport.

BricsCAD owns the live DWG database, document/editor lifecycle, native viewport, selection and CAD transactions. QS3D adds commands, Ribbon/palettes/modeless WPF UI, semantic/project state, deterministic quantity logic and guarded native geometry through the BricsCAD API.

`QS3D.Core` intentionally has no BricsCAD assembly dependency for deterministic testing and reuse. That layering must not be described as a standalone QS3D application. See `docs/PRODUCT-BOUNDARY.md`.

## Source of truth

QS3D deliberately separates original/authoritative data from calculated data:

- **DWG** is the source of truth for CAD geometry.
- **`.qsdb`** is the source of truth for QS semantic metadata: project, zones, floors, families, element relationships, rules and semantic properties.
- **Rule catalog / deterministic regenerators** are the source of truth for calculated quantities.
- **BQ / Excel / cached quantities** are derived outputs and may always be rebuilt.

Persistent CAD references use drawing identity + hexadecimal entity handles. Runtime `ObjectId` values are never persisted as cross-session identity.

## Layering

```text
BricsCAD V25 host / BrxMgd / TD_Mgd
        │
        ▼
QS3D.BricsCAD.V25  (plugin DLL)
  Commands / Ribbon / PaletteSet
  selection + handle adapters
  LayerTable / Xref adapters
  CAD transaction services
        │
        ▼
QS3D.Core  (CAD-independent library)
  project domain
  geometry/unit policy
  dependency + regeneration
  semantic wall/room/opening rules
  model health / revision / audit
  reporting / XLSX
        │
        ▼
.qsdb
```

The core has no BricsCAD assembly dependency so deterministic calculations remain testable outside CAD; the shipping product remains hosted by BricsCAD.

## Lifecycle

1. user/CAD event changes the active drawing or QS element inside BricsCAD;
2. plugin adapter normalizes geometry into metres / square metres / cubic metres;
3. semantic element is updated and marked dirty;
4. dependency graph propagates dirty state;
5. deterministic regenerator recalculates quantities;
6. `.qsdb` is saved atomically on explicit save;
7. plugin UI/BQ is refreshed;
8. Model Health can report broken hosts, missing CAD handles, missing family/floor/zone/material and dirty elements.

## Transaction rule

CAD writes happen inside a BricsCAD document lock + database transaction. `.qsdb` uses a single-writer `.lock`, temporary file and backup replacement. A CAD transaction and a project save are intentionally separate until V25 runtime testing proves the recovery path; no code should silently pretend they are one distributed transaction.

## Current 3D wall path

`QS3DWALL` captures selected CAD geometry as semantic `ArchitecturalWall` elements. For selected **LINE** entities it additionally creates a native `Solid3d` box using the selected Family's `ThicknessM`, `HeightM`, optional `BottomOffsetM`, then stores the generated solid handle on the semantic element. Polyline corner/join generation is deliberately not claimed complete before V25 runtime regression.
