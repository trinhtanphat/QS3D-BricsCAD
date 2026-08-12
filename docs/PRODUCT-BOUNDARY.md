# QS3D product boundary — BricsCAD V25 + V26 plugin

**Decision locked:** 2026-08-10 (UTC+7)  
**Host-major extension:** 2026-08-12 (UTC+7)  
**Applies to:** current source, README, requirements, plans, UI/release/install docs, agent handoffs and future product wording.

## Product decision

QS3D remains a **BricsCAD-hosted plugin**, not a standalone CAD desktop EXE. Adding BricsCAD V26 support changes the managed-host build/runtime boundary; it does not change the shipping form.

Older wording such as “app”, “application”, “BLT-like”, “BLT-style”, “full-screen” or “giống BLT” must not be interpreted as a requirement for a standalone executable.

## Shipping form and supported host majors

QS3D is intentionally a **Windows x64 BricsCAD plugin** with host-specific managed assemblies:

- **BricsCAD V25:** `QS3D.BricsCAD.V25.dll`, C# / .NET Framework 4.8 (`net48`).
- **BricsCAD V26:** `QS3D.BricsCAD.V26.dll`, C# / .NET 8 (`net8.0-windows`).
- **Shared Core:** `QS3D.Core.dll`, `netstandard2.0`, used by both host-major adapters.

The V26 assembly is a real .NET 8 rebuild lane; a V25 DLL must never be relabeled or packaged as V26. Build, runtime, package, installer, updater and release tooling must keep host-major identity explicit and fail closed on V25/V26 cross-use.

The release package is expected to contain QS3D DLLs plus install/update/checksum/sample helpers. A `QS3D.exe` is **not** a required or expected product artifact.

BricsCAD owns the native DWG database, document/editor lifecycle, viewport, selection, transactions and native CAD/3D API. QS3D contributes commands, Ribbon tabs, palettes/modeless WPF windows, semantic/project data, quantity/reporting, recognition and guarded generated geometry workflows inside the BricsCAD host process.

## `QS3D.Core` does not mean standalone

Keeping `QS3D.Core` independent from BricsCAD assemblies is deliberate for deterministic testing, reuse and host-major adapters. It does **not** imply that QS3D has its own DWG engine, CAD viewport or independent desktop process.

A future AutoCAD or other CAD adapter may reuse Core while still following the hosted-plugin model, but such support is not implied by the current V25/V26 boundary.

## BLT / BLT3D wording

BLT/BLT3D material is a clean-room **workflow and UX reference only**.

The phrases `BLT-like`, `BLT-style`, `BLT3D-familiar` and similar wording mean familiarity of navigation, commands, panels, quantity workflow and user experience. They do not claim knowledge of BLT packaging and do not establish any standalone-EXE requirement for QS3D.

No BLT source, binary, license file or proprietary asset is required by QS3D.

## UI boundary

All product UI targets are plugin UI hosted from BricsCAD:

- BricsCAD's native viewport remains the real 2D/3D canvas;
- QS3D Ribbon content is added to the BricsCAD Ribbon;
- Workspace/Right Panel are BricsCAD palettes;
- BQ, Health, Project Tools, Schedule, Curtain, Rebar and other WPF windows are modeless/plugin windows launched from BricsCAD;
- wording such as “full-screen BQ” means a large/modeless plugin window, not a separate desktop application shell.

V25 and V26 may require different managed-host binaries, but they intentionally share the same product workflows and source-of-truth contracts unless a host API difference forces an explicit adapter boundary.

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

An installer, updater, PowerShell helper, ZIP package or future bootstrapper does not turn the product into a standalone CAD application.

## Qualification boundary

Source/static/Core tests do not prove a host release. Production readiness is major-specific:

- V25 evidence follows `docs/LOCAL-V25-QUALIFICATION.md`.
- V26 evidence follows `docs/LOCAL-V26-QUALIFICATION.md` and must prove the .NET 8 host, exact V26 DLL, NETLOAD/DemandLoad, WPF/Ribbon/palettes, representative CAD operations, save/reopen, multi-DWG lifecycle and package/install/update behavior on the exact release SHA.

No remote agent may manufacture a licensed BricsCAD runtime, Authenticode-signing or clean-machine PASS that was not actually executed.

## Out of scope unless the owner explicitly reopens it

The current product plan does not include:

- a standalone `QS3D.exe` CAD application;
- a QS3D-owned DWG rendering/editing engine;
- a replacement for BricsCAD's viewport/database/editor;
- treating a V25 binary/package as V26-compatible without a V26 rebuild and qualification;
- silently changing the product into a launcher and describing it as standalone.

Any future change to one of those items requires an explicit owner requirement plus coordinated updates to requirements, architecture, build target, licensing, release/install flow and runtime validation.

## Documentation and agent rule

When describing the shipping product, prefer **plugin**, **module**, **palette**, **modeless window** or **tool** where accurate. Avoid an unqualified “QS3D application” when it could imply a separate EXE.

Historical audit/session documents remain valid as history, but this product-boundary decision and current source take precedence over ambiguous historical wording. Future agents must read this file before making packaging/architecture assumptions.

## Source evidence

The current repository architecture matches this decision:

- `src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj` is the V25 `net48` library adapter;
- `src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj` is the V26 `net8.0-windows` library adapter using V26 managed references;
- `PluginEntry` implements BricsCAD/Teigha `IExtensionApplication` in each host assembly;
- V25 and V26 package/runtime tooling uses explicit major-specific identities and external BricsCAD references;
- packaging intentionally excludes proprietary BricsCAD managed assemblies;
- no standalone QS3D desktop entry point is part of the current product target.
