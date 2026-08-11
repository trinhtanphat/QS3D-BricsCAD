# QS3D for BricsCAD V25

## Product form — BricsCAD plugin, not standalone EXE

QS3D is a clean-room **BIM / quantity takeoff / semantic 3D plugin for BricsCAD V25 x64**. It runs inside BricsCAD through `QS3D.BricsCAD.V25.dll`; it is not a standalone CAD application.

BricsCAD V25 is required at runtime.

The repository intentionally excludes BLT/BLT3D source or binaries, proprietary BricsCAD assemblies, customer drawings and private project data. BLT-style references describe workflow familiarity only; QS3D remains an independent implementation.

## Current status — 2026-08-11

The codebase is beyond prototype stage and contains broad source-side coverage for project data, semantic authoring, 3D generation, quantity/reporting, review, rebar and model-health workflows. The project is under active multi-agent development, so **source presence is not the same as production/runtime qualification**.

A production claim still requires the exact release SHA to be built and exercised on a licensed Windows x64 BricsCAD V25 workstation with representative drawings. See [`docs/LOCAL-V25-QUALIFICATION.md`](docs/LOCAL-V25-QUALIFICATION.md) and [`docs/HEALTH-AND-PREFLIGHT.md`](docs/HEALTH-AND-PREFLIGHT.md).

## What QS3D contains

### Semantic BIM/QS core

- Project, Zone, Floor/Level, Family/Type and Element model.
- `.qsdb` persistence, migration, audit/revision data and deterministic regeneration.
- Drawing-bound project lifecycle, selection synchronization and generated-handle ownership.
- Model Health and Release Readiness checks for semantic/source/generated consistency.

### Authoring and 3D

- Semantic capture for architectural, structural, room/opening and related QS categories.
- Direct Draw workflows for common wall/beam/slab/column and extended structural/architectural families.
- Plan-to-3D / guarded native `Solid3d` generation with rollback and ownership checks.
- Door/opening host links, room/finish workflows, Curtain Wall generation and source review tools.
- Rebar 3D workflows for columns, beams, stirrups/ties, slabs, structural walls and foundations.

### Quantity, schedules and deliverables

- BQ/quantity review, filtering, recalculation and CAD locate/reveal flows.
- Quick Takeoff and B4D-assisted recognition/review paths.
- Schedule Hub and domain schedules for quantities, finishes, materials, openings/doors, Curtain and rebar/BBS.
- XLSX/CSV deliverables with drawing/element traceability where supported by the workflow.

### BricsCAD UX

- Ribbon, Workspace palette, Project Tools, Domain Hub, Schedule Hub and Rebar 3D Hub.
- Modeless WPF tools tied to the owning drawing instead of silently mutating the active document after a DWG switch.
- Review commands for locate/highlight/focus/isolate/section workflows.
- Start/readiness surfaces and health-oriented workflow entry points are maintained as source evolves.

For the exact command inventory, use [`docs/COMMANDS.md`](docs/COMMANDS.md) rather than duplicating the complete list here.

## Architecture

```text
src/QS3D.Core/                 CAD-independent domain, persistence, quantities, diagnostics
src/QS3D.BricsCAD.V25/         BricsCAD V25 adapter, commands, CAD services, WPF and Ribbon
tests/QS3D.Core.SmokeTests/    deterministic Core regression/smoke coverage
scripts/                       repository preflights, packaging/install/update/runtime helpers
samples/generated/             repository-owned synthetic fixtures only
docs/                          product, architecture, workflows, qualification and handoff docs
```

Primary technology targets:

- BricsCAD V25 on Windows x64;
- plugin adapter: C# / .NET Framework 4.8 / WPF / BricsCAD .NET API;
- Core: `netstandard2.0`;
- source of truth: DWG source geometry plus `.qsdb` semantic/project metadata.

The canonical product boundary is [`docs/PRODUCT-BOUNDARY.md`](docs/PRODUCT-BOUNDARY.md); architecture details are in [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md).

## Repository validation

Run the repository/source guards before feature-specific work:

```bash
python scripts/preflight.py
python scripts/preflight-all.py
```

`preflight.py` owns generic repository/source policy. `preflight-all.py` discovers feature `preflight-*.py` guards, including the repository-health regression that parses all Python tooling and protects cross-platform private-artifact/manual-CI checks.

Core-only validation does not require proprietary BricsCAD binaries:

```bash
dotnet build src/QS3D.Core/QS3D.Core.csproj -c Release
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

The BricsCAD adapter must resolve V25 managed assemblies externally; proprietary assemblies must not be committed into this repository. GitHub Actions are manual-only unless the repository CI policy is explicitly changed. See [`CI_POLICY.md`](CI_POLICY.md) and [`docs/CI.md`](docs/CI.md).

GitHub Actions remain manual-only through `workflow_dispatch`; `release-v25.yml` requires owner-approved `RELEASE` confirmation.

## Runtime/release truth

Static checks and Core smoke tests can validate source contracts, deterministic logic and regression registration. They **cannot** by themselves prove:

- exact V25 managed-API compatibility;
- `NETLOAD` / DemandLoad behavior;
- native `Solid3d` authoring/boolean robustness on real drawings;
- multi-DWG/modeless UI lifecycle under the real host;
- Ribbon/WPF DPI and visual behavior;
- signed installer/update rollback behavior;
- large-project performance.

Those gates belong to local V25 qualification. Do not mark a release production-ready until the exact candidate SHA has runtime evidence.

## Documentation

Start at [`docs/README.md`](docs/README.md) for the compact documentation map.

Key durable references:

- [`docs/PRODUCT-BOUNDARY.md`](docs/PRODUCT-BOUNDARY.md) — product/hosting boundary;
- [`docs/SOURCE-OF-TRUTH.md`](docs/SOURCE-OF-TRUTH.md) — canonical data/source rules;
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — architecture;
- [`docs/COMMANDS.md`](docs/COMMANDS.md) — command/workflow catalog;
- [`docs/HEALTH-AND-PREFLIGHT.md`](docs/HEALTH-AND-PREFLIGHT.md) — health and source gates;
- [`docs/LOCAL-V25-QUALIFICATION.md`](docs/LOCAL-V25-QUALIFICATION.md) — runtime qualification;
- [`docs/AGENT-WORK-REGISTRATION.md`](docs/AGENT-WORK-REGISTRATION.md) — multi-agent claim protocol.

## Multi-agent contribution rule

This repository is edited concurrently. Before substantive work, follow [`AGENTS.md`](AGENTS.md) and register a non-overlapping ACTIVE claim under `docs/agent-work-claims/`. Re-read current `main` before writing, do not overwrite another active lane, and close the claim with validation evidence after the work is pushed.

## Clean-room policy

Only repository-owned synthetic fixtures may be committed for CAD/document regression. Do not commit private/customer DWGs, reference vendor projects, proprietary BricsCAD assemblies or third-party source/binaries that the project is not licensed to redistribute.
