# QS3D for BricsCAD V25 + V26

[English](README.md) | [Tiếng Việt](README.vi.md) | [Русский](README.ru.md) | [简体中文](README.zh-CN.md) | [日本語](README.ja.md)

QS3D is a clean-room **BIM, semantic 3D, coordination and quantity-takeoff plugin for BricsCAD V25 and V26 x64**. It runs inside BricsCAD as a managed plugin; it is not a standalone CAD executable.

> **Review snapshot — 2026-08-31:** this README was refreshed against `main` baseline `74a6aee92fc7066857e429b37fa2ff80e045ed9e`. The repository is under active concurrent development, so use the current `main`, [`docs/README.md`](docs/README.md), [`docs/COMMANDS.md`](docs/COMMANDS.md), and exact-SHA CI/runtime evidence when a release claim matters.

> **Product family:** this repository is the BricsCAD-hosted QS3D product. Shared vendor-neutral code is developed in sibling `trinhtanphat/QS3D-Platform`; the separate standalone desktop product is `trinhtanphat/QS3D-CAD`. See [`docs/PRODUCT-BOUNDARY.md`](docs/PRODUCT-BOUNDARY.md) and [`docs/QS3D-PLATFORM-MIGRATION.md`](docs/QS3D-PLATFORM-MIGRATION.md).

## What is in the repository

| Layer | Target | Responsibility |
| --- | --- | --- |
| `QS3D.Core` | `netstandard2.0` | CAD-independent domain model, persistence, geometry/quantity logic, diagnostics, reporting and application services |
| `QS3D.BricsCAD.V25` | .NET Framework 4.8 / x64 | BricsCAD V25 host adapter, commands, WPF UI and CAD integration |
| `QS3D.BricsCAD.V26` | `net8.0-windows` / x64 | BricsCAD V26 host build with V26-specific host/update boundaries while reusing compatible application source |
| `external/QS3D-Platform` | pinned submodule | Shared vendor-neutral contracts and platform code used by `QS3D.Core` |
| `tests/` | multiple test executables/projects | Deterministic Core regression, architecture, host/runtime harnesses and focused contract tests |
| `scripts/` + `.github/workflows/` | Python/PowerShell/YAML | Preflight, packaging, install/update, CI, release and runtime-proof tooling |

`QS3D.Core` references the pinned `external/QS3D-Platform` submodule for shared vendor-neutral contracts and platform code.

A matching licensed BricsCAD installation is required for host builds and runtime qualification. Proprietary BricsCAD SDK binaries, customer drawings, private project data and third-party product source are intentionally not committed.

## Capability map

The repository is well beyond a prototype, but capability maturity is not uniform. For individual command maturity, use the authoritative [`docs/COMMANDS.md`](docs/COMMANDS.md) instead of treating this overview as a certification list.

### Semantic BIM and project model

- Project, Zone, Floor/Level, Family/Type and semantic Element state.
- Drawing-bound project lifecycle, source/generated CAD-handle ownership and project metadata.
- Dependency, dirty/freshness, regeneration, persistence and recovery contracts.
- Project Browser / Workspace / Project Tools synchronization.
- Model Health, preflight and release-readiness surfaces.

### Structural authoring and 3D

- Direct-draw and semantic workflows for columns, beams, slabs, walls, openings and related structural/architectural families.
- Foundation workflows, including current single-footing source/proof hardening.
- Plan-to-3D and guarded native `Solid3d` generation with ownership/rollback checks.
- Rebar 3D workflows for beams, columns, slabs, structural walls and foundations.
- Steel detailing, weld/BOM and structural CSV/reporting surfaces.

### Quantity, schedules and deliverables

- Quantity/BQ review, filtering, recalculation, locate/reveal and model-evidence flows.
- Quick Takeoff and assisted recognition/review paths.
- Schedule Hub and domain schedules for quantities, finishes, materials, doors/openings, curtain systems and reinforcement/BBS.
- XLSX/CSV deliverables with element/source provenance where the workflow supports it.
- Cost, reporting, design-report and project-information surfaces.

### MEP and coordination

- MEP equipment/light/wire authoring, tagging, templates, schema/readiness and host-export workflows.
- Coordination/clash workflows, zones, dashboards and issue persistence.
- BCF import/export and external-clash exchange surfaces.
- HTTP CAD-worker, PostgreSQL/Supabase/RLS, RabbitMQ and object-storage integration code exists in the broader architecture; live external-service availability is environment-specific and must not be inferred from source presence.

### BIM interchange, planning and review

- IFC and JSON import/export paths, with maturity recorded per command in `docs/COMMANDS.md`.
- Planning/task links, task lists/export, 4D and animation/reporting surfaces.
- Ribbon, Workspace palette, Project Tools, Domain/Schedule/Rebar hubs and modeless WPF tools.
- Highlight/focus/isolate/section-style review and drawing-affinity safety paths.

### Experimental/integration web surface

The repository also contains web/integration test surfaces such as health/settings/project/document/quantity/cost APIs and viewer/bridge validation. These are integration surfaces around the QS3D product family; they do **not** turn this BricsCAD plugin repository into a standalone CAD replacement.

## Evidence and qualification model

The most important rule in this repository is simple:

> **Implemented in source is not the same as production-qualified in a licensed BricsCAD host.**

Keep these evidence classes separate:

| Evidence | What it can prove | What it does not prove |
| --- | --- | --- |
| Static/source preflight | source shape, policy, security/package contracts, deterministic regressions | native BricsCAD runtime behavior |
| Deterministic Core tests | CAD-independent domain, persistence, geometry, quantity, dependency and interchange behavior | `NETLOAD`, WPF/Ribbon or native CAD API behavior |
| Host build | compile compatibility with the selected BricsCAD SDK/major | successful licensed runtime execution |
| Licensed host proof | exact-major runtime behavior for the tested SHA and scenario | other host majors, other drawings or untested environments |

Current project history includes substantial source/preflight/Core/build evidence, while some exact licensed-host lanes can remain blocked by machine licensing/COM/UI/environment constraints. Do not report those blocked cells as runtime PASS.

Use [`docs/LOCAL-V25-QUALIFICATION.md`](docs/LOCAL-V25-QUALIFICATION.md), [`docs/LOCAL-V26-QUALIFICATION.md`](docs/LOCAL-V26-QUALIFICATION.md), runtime runbooks and exact-SHA artifacts for product qualification.

## Architecture and source-sharing model

```text
src/
  QS3D.Core/                 CAD-independent domain/application logic
  QS3D.BricsCAD.V25/         V25 net48/x64 BricsCAD + WPF host implementation
  QS3D.BricsCAD.V26/         V26 net8.0-windows/x64 host project

external/QS3D-Platform/      pinned shared platform submodule
tests/                       deterministic and host-oriented test projects
scripts/                     preflight, build, package, install/update and proof helpers
docs/                        architecture, workflow, policy and qualification documentation
.github/workflows/            automatic validation and controlled release/runtime workflows
```

V25 is the established .NET Framework adapter. V26 is a real .NET 8 host build, not a renamed V25 binary. The V26 project reuses compatible V25 application/UI source while keeping host-specific entry/update boundaries separate. Therefore **V25 evidence is never automatically V26 evidence**, and the reverse is also true.

`QS3D.Core` is intended to remain CAD-independent. New vendor-neutral logic should prefer Core/Platform boundaries rather than leaking proprietary BricsCAD API dependencies into the domain layer.

## Persistence and source of truth

The `.qsdb` project sidecar is treated as product data, not a disposable cache. The codebase includes bounded input handling, identity/reference validation, save-time validation, atomic publication, backup/recovery, locking/revision checks and dirty/freshness contracts.

The practical source-of-truth model combines **DWG source geometry** with **`.qsdb` semantic/project metadata**. See [`docs/SOURCE-OF-TRUTH.md`](docs/SOURCE-OF-TRUTH.md).

## Quick start for contributors

### 1. Clone with the pinned submodule

```bash
git clone --recurse-submodules https://github.com/trinhtanphat/QS3D-BricsCAD.git
cd QS3D-BricsCAD
```

If you cloned without submodules:

```bash
git submodule sync --recursive
git submodule update --init --recursive
```

Before substantive edits, read:

- [`AGENTS.md`](AGENTS.md)
- [`docs/MAIN-WRITE-AUTHORIZATION.md`](docs/MAIN-WRITE-AUTHORIZATION.md)
- [`docs/AGENT-WORK-REGISTRATION.md`](docs/AGENT-WORK-REGISTRATION.md)
- [`CI_POLICY.md`](CI_POLICY.md)

### 2. Run repository preflights

```bash
python scripts/preflight.py
python scripts/preflight-all.py
```

### 3. Build and run CAD-independent smoke tests

```bash
dotnet build src/QS3D.Core/QS3D.Core.csproj -c Release
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

These commands do not require BricsCAD SDK binaries.

### 4. Build a host adapter

Do not commit `BrxMgd.dll`, `TD_Mgd.dll` or other proprietary BricsCAD binaries.

V25 example:

```powershell
$env:BRICSCAD_V25_DIR = '<BricsCAD V25 installation directory>'
dotnet build src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj -c Release -p:Platform=x64
```

V26 example:

```powershell
$env:BRICSCAD_V26_DIR = '<BricsCAD V26 installation directory>'
dotnet build src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj -c Release -p:Platform=x64
```

Never point one host-major project at another major's SDK assemblies.

## Installation and loading

For end users, prefer a release bundle and its included installer/checksum instructions rather than copying arbitrary build output. See the repository's **Releases** page and host-specific release documentation.

For V25 browser-downloaded packages, Windows Mark-of-the-Web can block managed dependencies before QS3D startup executes. Prefer `INSTALL-QS3D.cmd` from the extracted package. For deliberate troubleshooting with direct `NETLOAD`, use the package-provided `UNBLOCK-QS3D.cmd` only after verifying that it belongs to the same release package.

Do not weaken BricsCAD trusted-path/security settings as a substitute for fixing package provenance or integrity.

## Command discovery

QS3D contains many operational, authoring, structural, MEP, coordination, quantity, schedule and interchange commands. The maintained catalog is:

- [`docs/COMMANDS.md`](docs/COMMANDS.md) — command names, purpose and maturity;
- [`docs/README.md`](docs/README.md) — documentation entry point;
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — architecture map.

Useful operational entry points include status/install/preflight/health commands; do not duplicate the complete catalog in this README because it changes frequently.

## CI, PR and merge policy

The current CI model is automatic for task branches and protected PRs:

- pushes to `agent/**` and `integration/**` are eligible for shared `.github/workflows/ci.yml` validation;
- PRs receive stable required contexts `preflight` and `core`;
- docs/repository-metadata-only changes use a lightweight tier;
- source/build-relevant changes use stronger source/Core/V25 validation as classified by changed paths;
- release/runtime publishing workflows remain separate controlled lanes.

A green check qualifies only the exact candidate it tested. Hosted CI does not create licensed-BricsCAD runtime evidence by itself.

Normal repository work uses:

```text
Issue / Reservation v2
  -> agent/<globally-distinct-session-token>/issue-<N>-<scope>
  -> implement + validate
  -> canonical PR
  -> fresh required checks
  -> merge same task PR when current, green, mergeable and collision-clean
  -> verify main + close/release task state
```

There is **no direct-main exception for documentation**. See [`docs/MAIN-WRITE-AUTHORIZATION.md`](docs/MAIN-WRITE-AUTHORIZATION.md) and [`CI_POLICY.md`](CI_POLICY.md).

## Engineering areas with the highest cross-cutting risk

A repository-wide review highlights several areas where changes deserve focused regression evidence:

- V25/V26 shared host source and framework/runtime compatibility.
- Drawing ownership, multi-DWG state and modeless WPF lifecycle.
- Native geometry/boolean operations and source/generated object ownership.
- `.qsdb` identity, dirty/freshness, atomic save and recovery semantics.
- Quantity/export provenance and XLSX/CSV integrity.
- Installer/update/package-origin and host-major isolation.
- External service integrations and environment-specific credentials/connectivity.

These are design constraints, not automatic blockers; they are simply the highest-leverage places to preserve focused tests and runtime proof.

## Documentation map

Start with [`docs/README.md`](docs/README.md). Important references include:

- [`docs/PRODUCT-BOUNDARY.md`](docs/PRODUCT-BOUNDARY.md) — product/hosting boundary.
- [`docs/QS3D-PLATFORM-MIGRATION.md`](docs/QS3D-PLATFORM-MIGRATION.md) — shared Platform/CAD boundary and migration.
- [`docs/SOURCE-OF-TRUTH.md`](docs/SOURCE-OF-TRUTH.md) — DWG/semantic source-of-truth rules.
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — architecture and dependency map.
- [`docs/COMMANDS.md`](docs/COMMANDS.md) — authoritative command catalog.
- [`docs/HEALTH-AND-PREFLIGHT.md`](docs/HEALTH-AND-PREFLIGHT.md) — health/preflight model.
- [`docs/LOCAL-V25-QUALIFICATION.md`](docs/LOCAL-V25-QUALIFICATION.md) — V25 runtime qualification.
- [`docs/LOCAL-V26-QUALIFICATION.md`](docs/LOCAL-V26-QUALIFICATION.md) — V26 runtime qualification.
- [`docs/MAIN-WRITE-AUTHORIZATION.md`](docs/MAIN-WRITE-AUTHORIZATION.md) — protected-main authorization.
- [`docs/AGENT-WORK-REGISTRATION.md`](docs/AGENT-WORK-REGISTRATION.md) — Reservation v2/canonical carrier workflow.
- [`CI_POLICY.md`](CI_POLICY.md) — current CI semantics.

## Releases and support boundary

Use GitHub **Releases** for packaged candidates and their exact release notes. A published package, a successful source build and a licensed-runtime qualification are separate evidence classes; read the release notes and attached proof for the exact host major you intend to run.

This repository does not distribute proprietary BricsCAD SDK/runtime binaries. Users and CI/runtime agents must provide their own valid BricsCAD installation and license where host execution is required.

## License

See [`LICENSE`](LICENSE) for the repository's license terms. Third-party and proprietary components remain subject to their own licenses.
