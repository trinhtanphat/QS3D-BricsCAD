# QS3D product boundary — BricsCAD V25 + V26 plugin

**Decision locked:** 2026-08-10 (UTC+7)  
**Host-major extension:** 2026-08-12 (UTC+7)  
**Sibling-product clarification:** 2026-08-13 (UTC+7)  
**Applies to:** current source, README, requirements, plans, UI/release/install docs, agent handoffs and future product wording.

## Product decision

This repository remains a **BricsCAD-hosted plugin**, not a standalone CAD desktop EXE. Adding BricsCAD V26 support changes the managed-host build/runtime boundary; it does not change the shipping form.

On 2026-08-13 the owner explicitly created sibling repositories `trinhtanphat/QS3D-Platform` and `trinhtanphat/QS3D-CAD`. That reopens standalone CAD as a **separate QS3D product-family effort**. It does **not** reopen or reverse this repository's hosted-plugin shipping decision.

Older wording such as “app”, “application”, “BLT-like”, “BLT-style”, “full-screen” or “giống BLT” must not be interpreted as a requirement for a standalone executable in this repository.

## QS3D product-family split

The canonical repository split is:

```text
                         QS3D-Platform
                    vendor-neutral shared layer
                  /                         \
                 /                           \
        QS3D-BricsCAD                      QS3D-CAD
        hosted plugin                 standalone CAD/BIM/QS
              |                               |
              v                               v
       BricsCAD V25/V26              QS3D-owned desktop host
```

Responsibilities:

- **`QS3D-BricsCAD` (this repository):** BricsCAD V25/V26 plugin, host adapters, BricsCAD commands/UI/runtime integration, plugin packaging/update and host-major qualification.
- **`QS3D-Platform`:** clean-room vendor-neutral domain, geometry value objects, semantic BIM/QS logic, quantity/persistence/application contracts and CAD-host abstractions. Shared contracts intended for V25 must remain consumable by `net48`, currently through `netstandard2.0` targeting.
- **`QS3D-CAD`:** separate standalone desktop product. It owns its own application shell and future legally licensed native drawing/geometry/render adapters. Its runtime evidence is independent from BricsCAD evidence.

See [`QS3D-PLATFORM-MIGRATION.md`](QS3D-PLATFORM-MIGRATION.md) for the incremental migration plan. Existing `QS3D.Core` code remains authoritative for this plugin until an individual migration slice has parity evidence and the consumer is deliberately switched.

## Shipping form and supported host majors

QS3D in this repository is intentionally a **Windows x64 BricsCAD plugin** with host-specific managed assemblies:

- **BricsCAD V25:** `QS3D.BricsCAD.V25.dll`, C# / .NET Framework 4.8 (`net48`).
- **BricsCAD V26:** `QS3D.BricsCAD.V26.dll`, C# / .NET 8 (`net8.0-windows`).
- **Current shared Core:** `QS3D.Core.dll`, `netstandard2.0`, used by both host-major adapters while migration to sibling `QS3D-Platform` proceeds incrementally.

The V26 assembly is a real .NET 8 rebuild lane; a V25 DLL must never be relabeled or packaged as V26. Build, runtime, package, installer, updater and release tooling must keep host-major identity explicit and fail closed on V25/V26 cross-use.

The release package from **this repository** is expected to contain QS3D plugin DLLs plus install/update/checksum/sample helpers. A standalone `QS3D.exe` is not a required or expected artifact of the BricsCAD package; the standalone executable belongs to the sibling `QS3D-CAD` product.

BricsCAD owns the native DWG database, document/editor lifecycle, viewport, selection, transactions and native CAD/3D API for this plugin. QS3D contributes commands, Ribbon tabs, palettes/modeless WPF windows, semantic/project data, quantity/reporting, recognition and guarded generated geometry workflows inside the BricsCAD host process.

## `QS3D.Core` / `QS3D-Platform` does not mean this plugin is standalone

Keeping host-neutral code independent from BricsCAD assemblies is deliberate for deterministic testing, reuse and multiple host adapters. It does **not** imply that the BricsCAD package has its own DWG engine, CAD viewport or independent desktop process.

Shared semantics may progressively move from `QS3D.Core` into sibling `QS3D-Platform`, but the BricsCAD runtime boundary stays behind an adapter. A Platform library reference must never become a hidden route for proprietary BricsCAD or standalone native-SDK types to cross product boundaries.

## BLT / BLT3D wording

BLT/BLT3D material is a clean-room **workflow and UX reference only**.

The phrases `BLT-like`, `BLT-style`, `BLT3D-familiar` and similar wording mean familiarity of navigation, commands, panels, quantity workflow and user experience. They do not claim knowledge of BLT packaging and do not establish any standalone-EXE requirement for this repository.

No BLT source, binary, license file or proprietary asset is required by QS3D.

## UI boundary

All UI targets shipped from this repository are plugin UI hosted from BricsCAD:

- BricsCAD's native viewport remains the real 2D/3D canvas;
- QS3D Ribbon content is added to the BricsCAD Ribbon;
- Workspace/Right Panel are BricsCAD palettes;
- BQ, Health, Project Tools, Schedule, Curtain, Rebar and other WPF windows are modeless/plugin windows launched from BricsCAD;
- wording such as “full-screen BQ” means a large/modeless plugin window, not a separate desktop application shell.

`QS3D-CAD` may implement analogous standalone workspaces using its own desktop shell. Similar UX does not make the two host lifecycles interchangeable.

V25 and V26 may require different managed-host binaries, but they intentionally share the same plugin workflows and source-of-truth contracts unless a host API difference forces an explicit adapter boundary.

## Install and release boundary

Normal V25 flow:

```text
Install/register QS3D V25 package
→ start BricsCAD V25
→ BricsCAD DemandLoads QS3D.BricsCAD.V25.dll on command (or user NETLOADs it)
→ use QS3D inside BricsCAD
```

Normal V26 flow:

```text
Install/register QS3D V26 package
→ start BricsCAD V26 with .NET 8 Desktop Runtime available
→ BricsCAD DemandLoads QS3D.BricsCAD.V26.dll on command (or user NETLOADs it)
→ use QS3D inside BricsCAD
```

Package/update identities are major-specific:

- V25: `QS3D-BricsCAD-V25.zip` / `QS3D-BricsCAD-V25.update.json`.
- V26: `QS3D-BricsCAD-V26.zip` / `QS3D-BricsCAD-V26.update.json`.

Installers/updaters must target only the matching BricsCAD registry major and managed DLL. They must not weaken BricsCAD security settings and must retain package-hash, ownership, signature, transactional rollback and version checks.

An installer, updater, PowerShell helper, ZIP package or future bootstrapper from this repository does not turn the plugin into the sibling standalone CAD product.

## Qualification boundary

Source/static/Core/Platform tests do not prove a host release. Production readiness is major-specific:

- V25 evidence follows `docs/LOCAL-V25-QUALIFICATION.md`.
- V26 evidence follows `docs/LOCAL-V26-QUALIFICATION.md` and must prove the .NET 8 host, exact V26 DLL, NETLOAD/DemandLoad, WPF/Ribbon/palettes, representative CAD operations, save/reopen, multi-DWG lifecycle and package/install/update behavior on the exact release SHA.

No remote agent may manufacture a licensed BricsCAD runtime, Authenticode-signing or clean-machine PASS that was not actually executed.

Standalone evidence is separate again: an in-memory or source-level `QS3D-CAD` PASS is not native DWG/runtime qualification, and standalone native evidence can never substitute for V25/V26 BricsCAD qualification.

## Out of scope for this repository

The following remain outside `QS3D-BricsCAD` even though sibling `QS3D-CAD` may implement them:

- shipping a standalone `QS3D.exe` from this repository;
- owning the standalone DWG rendering/editing engine here;
- replacing BricsCAD's viewport/database/editor inside the BricsCAD plugin;
- copying standalone native SDK binaries/types into this repository's shared Core boundary;
- treating a V25 binary/package as V26-compatible without a V26 rebuild and qualification;
- silently changing the plugin into a launcher and describing it as standalone.

A future change to any of these **repository-local** boundaries requires an explicit owner requirement plus coordinated updates to requirements, architecture, build target, licensing, release/install flow and runtime validation. The existence of `QS3D-CAD` alone is not that change.

## Documentation and agent rule

When describing the product shipped by this repository, prefer **plugin**, **module**, **palette**, **modeless window** or **tool** where accurate. Avoid an unqualified “QS3D application” when it could imply that this repository ships the standalone EXE.

When discussing the whole product family, qualify names explicitly as `QS3D-BricsCAD`, `QS3D-Platform` and `QS3D-CAD`.

Historical audit/session documents remain valid as history, but this product-boundary decision and current source take precedence over ambiguous historical wording. Future agents must read this file and `docs/QS3D-PLATFORM-MIGRATION.md` before making cross-repository packaging/architecture assumptions.

## Source evidence

The current repository architecture matches this decision:

- `src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj` is the V25 `net48` library adapter;
- `src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj` is the V26 `net8.0-windows` library adapter using V26 managed references;
- `PluginEntry` implements BricsCAD/Teigha `IExtensionApplication` in each host assembly;
- V25 and V26 package/runtime tooling uses explicit major-specific identities and external BricsCAD references;
- packaging intentionally excludes proprietary BricsCAD managed assemblies;
- no standalone QS3D desktop entry point is part of this repository's current product target.
