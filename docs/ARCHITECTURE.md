# QS3D architecture — BricsCAD V25 + V26

## Hosted-plugin boundary

QS3D is a **BricsCAD-hosted plugin**, not a standalone CAD executable. Two Windows x64 host lanes are maintained:

- `QS3D.BricsCAD.V25` — BricsCAD V25, `net48` / .NET Framework 4.8;
- `QS3D.BricsCAD.V26` — BricsCAD V26, `net8.0-windows` / .NET 8 Windows Desktop.

BricsCAD owns the live DWG database, document/editor lifecycle, native viewport, selection and CAD transactions. QS3D adds commands, Ribbon/palettes/modeless WPF UI, semantic/project state, deterministic quantity logic and guarded native geometry through the BricsCAD API.

`QS3D.Core` intentionally has no BricsCAD assembly dependency. That separation supports deterministic tests and multiple host adapters; it does **not** make QS3D a standalone CAD application. See `docs/PRODUCT-BOUNDARY.md`.

## Host-adapter strategy

V25 remains the established source adapter. V26 rebuilds the same proven CAD/WPF adapter source under the .NET 8 host instead of maintaining a second feature fork.

```text
BricsCAD V25 / .NET Framework 4.8
        │
        ▼
QS3D.BricsCAD.V25.dll
        │
        ├──────── shared CAD/WPF source ────────┐
        │                                       │
BricsCAD V26 / .NET 8 Windows Desktop          │
        │                                       │
        ▼                                       │
QS3D.BricsCAD.V26.dll ◄─────────────────────────┘
        │
        ▼
QS3D.Core  (netstandard2.0, CAD-independent)
        │
        ▼
.qsdb
```

The V26 project keeps the established `QS3D.BricsCAD.V25` source namespace so linked XAML/classes do not fork. It preserves nullable annotations without reinterpreting the different V26 host API metadata as new flow errors for the established V25 adapter. It emits a distinct assembly name and resolves `BrxMgd.dll`, `TD_Mgd.dll` and `TD_MgdBrep.dll` only from `BRICSCAD_V26_DIR`; the BREP reference supports shared exact-face quantity source. BricsCAD-owned assemblies are external host references with `Private=false` and are never packaged into QS3D.

Use `QS3D.sln` for the existing V25-oriented solution and `QS3D.V26.sln` for the isolated V26/Core/SmokeTests build surface. The separate solution prevents a normal V25 solution build from requiring a V26 installation and vice versa.

## Host-major update isolation

V25 and V26 may share one GitHub Releases/tag history, but they are separate signed update channels:

- V25 membership requires `QS3D-BricsCAD-V25.update.json` and the V25 ZIP/target identity;
- V26 membership requires `QS3D-BricsCAD-V26.update.json` and the V26 ZIP/target identity.

Release discovery filters by the exact host-major manifest asset **before** latest-version selection. V26 has host-specific manifest validation and secure-launcher surfaces while reusing the host-neutral update lifecycle/UI/SemVer source. Cross-major manifest, package, mutex, install path or plugin identity is fail-closed.

## Source of truth

QS3D deliberately separates original/authoritative data from calculated data:

- **DWG** is the source of truth for CAD geometry.
- **`.qsdb`** is the source of truth for QS semantic metadata: project, zones, floors, families, element relationships, rules and semantic properties.
- **Rule catalog / deterministic regenerators** are the source of truth for calculated quantities.
- **BQ / Excel / cached quantities** are derived outputs and may always be rebuilt.

Persistent CAD references use drawing identity + hexadecimal entity handles. Runtime `ObjectId` values are never persisted as cross-session identity.

## Core layering

```text
BricsCAD host / BrxMgd / TD_Mgd
        │
        ▼
QS3D host adapter
  Commands / Ribbon / PaletteSet
  selection + handle adapters
  LayerTable / Xref adapters
  CAD transaction services
        │
        ▼
QS3D.Core
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

The Core library remains CAD-independent so deterministic calculations and persistence contracts can be tested outside BricsCAD.

## Lifecycle

1. a user/CAD event changes the active drawing or QS element inside BricsCAD;
2. the host adapter normalizes geometry into metres / square metres / cubic metres;
3. semantic state is updated and marked dirty;
4. dependency graph propagates dirty state;
5. deterministic regenerators recalculate derived quantities/geometry state;
6. `.qsdb` is persisted through its atomic/recovery-safe store/session contracts;
7. plugin UI/BQ is refreshed;
8. Model Health can report broken hosts, missing CAD handles, missing family/floor/zone/material and dirty elements.

## Transaction rule

CAD writes happen inside a BricsCAD document lock + database transaction. `.qsdb` uses its own staged/atomic persistence and backup-recovery contracts. A CAD transaction and project persistence are separate durability domains; code must not silently pretend they are one distributed transaction.

## Runtime qualification boundary

Shared source does **not** imply shared runtime proof. V25 and V26 must each be qualified against the exact candidate assembly on their licensed host major. V26 additionally requires .NET 8 Windows Desktop runtime/SDK compatibility evidence.

See:

- `docs/LOCAL-V25-QUALIFICATION.md`;
- `docs/LOCAL-V26-QUALIFICATION.md`;
- `docs/MANUAL-BUILD-RELEASE.md` for V25 release operation;
- `docs/MANUAL-BUILD-RELEASE-V26.md` for V26 release operation.
