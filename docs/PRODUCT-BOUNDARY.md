# QS3D product boundary — BricsCAD V25 plugin

**Decision locked:** 2026-08-10 (UTC+7)  
**Applies to:** current source, README, requirements, plans, UI/release/install docs, agent handoffs and future product wording.

## Session decision

The current development session was re-reviewed after a packaging/architecture ambiguity surfaced: whether QS3D should be a standalone desktop EXE or a BricsCAD-hosted tool. The owner explicitly chose to **keep QS3D as a BricsCAD plugin**.

This decision resolves that ambiguity. Any older chat/history wording such as “app”, “application”, “BLT-like”, “BLT-style”, “full-screen” or “giống BLT” must not be interpreted as a requirement for a standalone executable.

## Shipping form

QS3D is intentionally a **BricsCAD V25 x64 .NET plugin**, not a standalone CAD desktop application.

- BricsCAD V25 is required at runtime.
- `QS3D.BricsCAD.V25` is a .NET Framework 4.8 **library/DLL** loaded into BricsCAD by DemandLoad or `NETLOAD`.
- `QS3D.Core` is a CAD-independent library for deterministic domain, geometry, quantity, persistence, reporting and test logic.
- The release package is expected to contain QS3D DLLs plus install/update/checksum/sample helpers. A `QS3D.exe` is **not** a required or expected product artifact.
- BricsCAD owns the native DWG database, document/editor lifecycle, viewport, selection, transactions and native CAD/3D API.
- QS3D contributes commands, Ribbon tabs, palettes/modeless WPF windows, semantic/project data, quantity/reporting, recognition and guarded generated geometry workflows inside the BricsCAD host process.

## `QS3D.Core` does not mean standalone

Keeping `QS3D.Core` independent from BricsCAD assemblies is deliberate for deterministic testing, reuse and future adapters. It does **not** imply that the current QS3D product has its own DWG engine, CAD viewport or independent desktop process.

A future AutoCAD or other CAD adapter may reuse Core while still following the same hosted-plugin model.

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

## Install and release boundary

A normal user flow is:

```text
Install/register QS3D plugin
→ start BricsCAD V25
→ BricsCAD DemandLoads QS3D on command (or user NETLOADs the DLL)
→ use QS3D Ribbon/palettes/commands inside BricsCAD
```

Release documentation must identify the DLL/DemandLoad form clearly. An installer, updater, PowerShell helper, ZIP package or future bootstrapper does not turn the product into a standalone CAD application.

## Out of scope unless the owner explicitly reopens it

The current product plan does not include:

- a standalone `QS3D.exe` CAD application;
- a QS3D-owned DWG rendering/editing engine;
- a replacement for BricsCAD's viewport/database/editor;
- silently changing the product into a launcher and describing it as standalone.

Any future change to one of those items requires an explicit owner requirement plus coordinated updates to requirements, architecture, build target, licensing, release/install flow and runtime validation. It must never be inferred merely from “giống BLT”.

## Documentation and agent rule

When describing the shipping product, prefer **plugin**, **module**, **palette**, **modeless window** or **tool** where accurate. Avoid an unqualified “QS3D application” when it could imply a separate EXE.

Historical audit/session documents remain valid as history, but this product-boundary decision and the current source take precedence over ambiguous historical wording. Future agents must read this file before making packaging/architecture assumptions.

## Source evidence

The current repository architecture already matches this decision:

- `src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj` uses `OutputType=Library`;
- the adapter references BricsCAD V25 managed assemblies (`BrxMgd.dll`, `TD_Mgd.dll`);
- `PluginEntry` implements BricsCAD/Teigha `IExtensionApplication`;
- packaging/install tooling ships DLLs and registers/loads them through DemandLoad/`NETLOAD`;
- no standalone QS3D desktop entry point is part of the current product target.
