# QS3D for BricsCAD V25 + V26

QS3D is a clean-room **BIM, semantic 3D and quantity-takeoff plugin for BricsCAD V25 and V26 x64**. It runs inside BricsCAD as a managed plugin; it is not a standalone CAD executable.

> **QS3D product family:** this repository remains the BricsCAD-hosted product. Shared vendor-neutral code is being developed in sibling `trinhtanphat/QS3D-Platform`, while the separate standalone desktop product is `trinhtanphat/QS3D-CAD`. See [`docs/PRODUCT-BOUNDARY.md`](docs/PRODUCT-BOUNDARY.md) and [`docs/QS3D-PLATFORM-MIGRATION.md`](docs/QS3D-PLATFORM-MIGRATION.md). The sibling standalone effort does not remove the licensed-BricsCAD requirement for this plugin.

| Layer | Target | Role |
| --- | --- | --- |
| `QS3D.Core` | `netstandard2.0` | CAD-independent domain model, persistence, geometry/quantity logic, diagnostics and reporting |
| `QS3D.BricsCAD.V25` | .NET Framework 4.8 / x64 | BricsCAD V25 host adapter, commands, WPF UI and CAD integration |
| `QS3D.BricsCAD.V26` | `net8.0-windows` / x64 | BricsCAD V26 .NET 8 host build with V26-specific host/update boundaries |

A matching licensed BricsCAD installation is required for host builds and runtime qualification. Proprietary BricsCAD assemblies, customer drawings, private project data and third-party product source/binaries are intentionally excluded from the repository.

## Current engineering status — 2026-08-12

The repository is well beyond a prototype: it contains broad source-side implementation for project data, semantic authoring, CAD generation, quantity/reporting, schedules, review, rebar, model health, persistence and release tooling.

The important qualification boundary is:

> **Implemented in source does not automatically mean production-qualified in BricsCAD.**

Static preflights and deterministic Core smoke tests can prove repository contracts and many regressions without proprietary SDK files. They cannot replace an exact-SHA build and runtime pass on the licensed BricsCAD major being released.

The repository is also under active concurrent development. `main` can move frequently. Normal AI agents/chat sessions must treat `main` as read-only unless the repository owner explicitly authorizes that session to merge/integrate a named PR or batch. Follow [`docs/MAIN-WRITE-AUTHORIZATION.md`](docs/MAIN-WRITE-AUTHORIZATION.md), [`AGENTS.md`](AGENTS.md) and [`docs/AGENT-WORK-REGISTRATION.md`](docs/AGENT-WORK-REGISTRATION.md).

## Product capabilities represented in the codebase

### Semantic BIM / QS project model

- Project, Zone, Floor/Level, Family/Type and semantic Element state.
- Drawing-bound project lifecycle and source/generated CAD-handle ownership.
- Dependency, dirty-state, regeneration and persistence metadata.
- Project Browser / Workspace / Project Tools synchronization.
- Model Health and Release Readiness checks for semantic, source and generated consistency.

### Authoring and 3D generation

- Semantic capture for architectural, structural, room/opening and related QS categories.
- Direct Draw workflows for common wall, beam, slab, column and extended domain families.
- Plan-to-3D and guarded native `Solid3d` generation with ownership/rollback checks.
- Door/opening host links, room/finish workflows, Curtain Wall generation and source review.
- Rebar 3D workflows covering columns, beams, stirrups/ties, slabs, structural walls and foundations.

### Quantity, schedules and deliverables

- Quantity/BQ review, filtering, recalculation and CAD locate/reveal flows.
- Quick Takeoff and B4D-assisted recognition/review paths.
- Schedule Hub plus domain schedules for quantities, finishes, materials, openings/doors, Curtain and rebar/BBS.
- XLSX/CSV deliverables with source/element traceability where supported by the workflow.

### BricsCAD UX

- Ribbon integration, Workspace palette, Project Tools, Domain Hub, Schedule Hub and Rebar 3D Hub.
- Modeless WPF tools with drawing-ownership guards so a DWG switch does not silently redirect edits to the wrong document.
- Locate/highlight/focus/isolate/section-style review commands.
- Start/readiness and health-oriented entry points.

For the authoritative command inventory, use [`docs/COMMANDS.md`](docs/COMMANDS.md) rather than duplicating every command here.

## Repository architecture

```text
src/
  QS3D.Core/                 CAD-independent domain, persistence, geometry,
                             quantities, diagnostics and reporting
  QS3D.BricsCAD.V25/         V25 net48 BricsCAD/WPF adapter and the main shared
                             host implementation
  QS3D.BricsCAD.V26/         V26 net8.0-windows host project and V26-specific
                             entry/update boundaries

tests/
  QS3D.Core.SmokeTests/      deterministic Core regression/smoke executable

scripts/                     preflight, package, install, update and runtime helpers
samples/generated/           repository-owned synthetic fixtures only
docs/                        architecture, product, workflow and qualification docs
```

### V25 / V26 source-sharing model

V25 is the established `net48` adapter. V26 is a real .NET 8 build lane, not a renamed V25 binary.

The V26 project deliberately **links most V25 C# and XAML source** while excluding/replacing host-specific entry and updater surfaces. This reduces feature drift between majors, but it also creates a deliberate compatibility coupling: shared host code must continue compiling and behaving correctly under both the .NET Framework V25 host and the .NET 8 V26 host.

For that reason, V25 runtime evidence must never be reported as V26 runtime evidence, or vice versa.

## Persistence and data-integrity posture

The `.qsdb` path is treated as product data rather than an incidental sidecar. The current implementation includes defensive boundaries such as:

- bounded project/XML input handling;
- hardened XML parsing and schema/current-state validation;
- canonical identity checks across persisted project/domain references;
- save-time validation before publication;
- atomic publication with backup/recovery behavior;
- project-file locking and revision/baseline checks around concurrent/stale saves;
- persistence stamps and dirty/freshness contracts used by save/regeneration flows.

The practical source of truth is **DWG source geometry plus `.qsdb` semantic/project metadata**. See [`docs/SOURCE-OF-TRUTH.md`](docs/SOURCE-OF-TRUTH.md) for the canonical rules.

## Update and release security posture

The update/package code is intentionally fail-safe rather than permissive. Current safeguards include host-major isolation, bounded downloads, secure release-origin checks, signed artifact/script verification where required by the release lane, manifest/package integrity checks and rollback-oriented update flow.

V25 and V26 package/update identities are kept separate. A V25 package or updater must not silently qualify as V26 simply because most application source is shared.

The in-plugin V26 update lane remains subject to V26-specific qualification; do not treat source presence alone as proof that one-click V26 updating is production-ready.

## V25 manual NETLOAD / Mark-of-the-Web recovery

If BricsCAD V25 reports `Could not load file or assembly ... Operation is not supported` (commonly .NET Framework HRESULT `0x80131515`) while `NETLOAD` is pointed at `QS3D.BricsCAD.V25.dll` in an extracted browser-downloaded package, Windows may still have Mark-of-the-Web (`Zone.Identifier`) on the plugin or one of its dependencies. That rejection occurs before QS3D startup code can run.

The recommended path is to run `INSTALL-QS3D.cmd` from the extracted V25 package and then start BricsCAD normally; the installer verifies package integrity and removes Mark-of-the-Web from the installed payload before DemandLoad uses it.

If direct `NETLOAD` is intentionally required for troubleshooting, run `UNBLOCK-QS3D.cmd` in the **same extracted V25 package** first. The launcher verifies the recovery helper hash before bootstrap, and the helper verifies complete `SHA256SUMS.txt` coverage plus the expected V25 package identity files before unblocking the whole package. It does not relax BricsCAD security/trusted-path settings and does not use `ExecutionPolicy Bypass`.

For a newly downloaded ZIP, another safe option is to right-click the ZIP in Windows Explorer, choose **Properties → Unblock**, apply it, and only then extract the package. Do not unblock only `QS3D.BricsCAD.V25.dll`; a dependency that remains blocked can produce the same loader failure.

## Quick start for contributors

### 1. Clone and inspect repository policy

```bash
git clone https://github.com/trinhtanphat/QS3D-BricsCAD.git
cd QS3D-BricsCAD
```

Before substantive edits, read:

- [`docs/MAIN-WRITE-AUTHORIZATION.md`](docs/MAIN-WRITE-AUTHORIZATION.md) — canonical `main` write/merge authorization rule;
- [`AGENTS.md`](AGENTS.md) — concurrent-editing and execution-scope rules;
- [`CI_POLICY.md`](CI_POLICY.md) — manual-by-default Actions policy plus the single approved automatic post-integration V25 dispatcher;
- [`docs/AGENT-WORK-REGISTRATION.md`](docs/AGENT-WORK-REGISTRATION.md) — Issue/branch/PR reservation and integration protocol.

For a normal AI agent/chat session, the default workflow is:

```text
read latest main
  -> check/create Issue
  -> create agent/<agent-id>/<scope>
  -> edit source/tests/scripts/workflows/docs/Markdown/chore on that branch
  -> validate
  -> commit + push branch
  -> open/update PR
  -> STOP BEFORE MERGE
```

Requests such as `fix bug`, `update code`, `implement all`, `continue all`, `commit push git`, `update docs`, `chore`, `run CI` or `fix CI` do **not** grant permission to push or merge `main`. Only an explicit owner instruction authorizing the named merge/integration does so.

### 2. Run repository preflights

```bash
python scripts/preflight.py
python scripts/preflight-all.py
```

`preflight.py` owns generic repository/source policy. `preflight-all.py` discovers the focused `preflight-*.py` gates used to protect feature, release, host-major and regression contracts.

### 3. Build and run Core smoke tests

These commands do not require BricsCAD SDK assemblies:

```bash
dotnet build src/QS3D.Core/QS3D.Core.csproj -c Release
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

The smoke executable covers far more than a startup check; its registered regressions span domain/persistence, geometry, quantities, dependency/freshness behavior, health, QSDB and interchange/export contracts.

### 4. Build a BricsCAD host adapter

The host projects reference the licensed BricsCAD installation externally. Do **not** commit `BrxMgd.dll`, `TD_Mgd.dll` or other proprietary BricsCAD binaries.

PowerShell example for V25:

```powershell
$env:BRICSCAD_V25_DIR = '<BricsCAD V25 installation directory>'
dotnet build src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj -c Release -p:Platform=x64
```

PowerShell example for V26:

```powershell
$env:BRICSCAD_V26_DIR = '<BricsCAD V26 installation directory>'
dotnet build src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj -c Release -p:Platform=x64
```

Never point the V25 project at V26 assemblies or the V26 project at V25 assemblies.

## Validation model

QS3D has three different evidence levels. Keep them separate in reviews and release notes.

### 1. Static/source validation

Examples: repository preflights, source-shape/security checks, package/update guards and policy checks.

Useful for finding deterministic source/policy regressions. **Not host-runtime evidence.**

### 2. Deterministic Core validation

`QS3D.Core.SmokeTests` exercises CAD-independent behavior and many regression cases without BricsCAD.

Useful for domain, persistence, geometry, quantity, dependency and interchange correctness. **Still not BricsCAD runtime evidence.**

### 3. Licensed BricsCAD runtime qualification

Required for claims about:

- exact V25/V26 managed API compatibility;
- `NETLOAD` / DemandLoad behavior;
- native `Solid3d` generation and boolean robustness on real drawings;
- multi-DWG and modeless UI lifecycle;
- Ribbon/WPF/DPI behavior;
- installer/update/signing behavior;
- large-project runtime performance.

Use [`docs/LOCAL-V25-QUALIFICATION.md`](docs/LOCAL-V25-QUALIFICATION.md) and [`docs/LOCAL-V26-QUALIFICATION.md`](docs/LOCAL-V26-QUALIFICATION.md) for the exact host-major gates.

## CI and release policy

GitHub Actions are **manual-only by default**. The sole owner-approved automatic exception is `.github/workflows/dispatch-v25-cloud-after-main-integration.yml`, which may react only to an integration-relevant authorized `main` landing and dispatch only `release-v25-cloud.yml`.

Normal commits, reviews, documentation updates, fixes and `continue all` requests do not authorize a manual Actions dispatch. Manual CI permission does not imply `main` merge permission, and `main` merge permission does not imply unrelated manual CI/release permission.

Ordinary docs/Markdown-only landings outside the dispatcher's watched paths do not trigger V25 cloud release CI. Changed paths are authoritative: a `chore:` commit that changes `src/**`, `tests/**`, `scripts/**`, build/solution files or watched workflows is still integration-relevant.

Release workflows require explicit release intent and their configured `RELEASE` confirmation. A production release should be tied to one exact candidate SHA and the matching host-major qualification evidence.

Representative workflows:

- `.github/workflows/ci.yml` — Core/static validation;
- `.github/workflows/bricscad-v25.yml` — licensed V25 integration/runtime lane;
- `.github/workflows/bricscad-v26.yml` — licensed V26 integration/runtime lane;
- `.github/workflows/release-v25.yml` — manual V25 package/release lane;
- `.github/workflows/release-v25-cloud.yml` — V25 cloud release workflow, manual directly or via the single approved dispatcher;
- `.github/workflows/dispatch-v25-cloud-after-main-integration.yml` — sole automatic post-integration dispatcher;
- `.github/workflows/release-v26.yml` — V26 package/signed-manifest/release lane.

The automatic cloud run is not licensed local BricsCAD runtime proof. NETLOAD/native UI/private-DWG/signing/performance gates remain separate evidence.

## Engineering constraints worth knowing

A repository-wide source review shows several deliberate trade-offs that future work should preserve or simplify carefully:

- **Shared V25/V26 host source:** reduces duplicate implementation, but every shared host edit has two framework/runtime compatibility surfaces.
- **Large host lifecycle surface:** drawing ownership, modeless windows, project save/recovery and generated CAD ownership interact heavily; regression tests should accompany lifecycle changes.
- **Persistence is correctness-critical:** canonical IDs, dirty/freshness state, atomic publication and stale-session detection are part of the product contract, not implementation details.
- **Many focused preflights:** they provide strong regression fences, but source-shape gates should stay aligned with intended behavior so they do not become accidental architecture locks.
- **Manual-by-default CI:** most workflows require deliberate owner dispatch; the single automatic V25 cloud dispatcher is deliberately narrow and still does not replace host qualification discipline.
- **Static review has a ceiling:** absence of obvious placeholders or a passing Core/preflight suite does not prove native CAD geometry, WPF lifecycle or updater behavior in a licensed host.

These are not blockers; they are the areas where changes have the highest cross-cutting risk.

## Documentation map

Start with [`docs/README.md`](docs/README.md). Durable references include:

- [`docs/MAIN-WRITE-AUTHORIZATION.md`](docs/MAIN-WRITE-AUTHORIZATION.md) — canonical `main` write/merge authorization;
- [`docs/PRODUCT-BOUNDARY.md`](docs/PRODUCT-BOUNDARY.md) — product/hosting boundary;
- [`docs/QS3D-PLATFORM-MIGRATION.md`](docs/QS3D-PLATFORM-MIGRATION.md) — sibling Platform/CAD ownership and incremental Core migration plan;
- [`docs/SOURCE-OF-TRUTH.md`](docs/SOURCE-OF-TRUTH.md) — canonical project/source rules;
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — architecture;
- [`docs/COMMANDS.md`](docs/COMMANDS.md) — command/workflow catalog;
- [`docs/HEALTH-AND-PREFLIGHT.md`](docs/HEALTH-AND-PREFLIGHT.md) — health and source gates;
- [`docs/LOCAL-V25-QUALIFICATION.md`](docs/LOCAL-V25-QUALIFICATION.md) — V25 runtime qualification;
- [`docs/LOCAL-V26-QUALIFICATION.md`](docs/LOCAL-V26-QUALIFICATION.md) — V26 runtime/package qualification;
- [`docs/AGENT-WORK-REGISTRATION.md`](docs/AGENT-WORK-REGISTRATION.md) — multi-agent reservation protocol.

## Clean-room policy

Only repository-owned synthetic fixtures may be committed for CAD/document regression. Do not commit private/customer DWGs, reference vendor projects, proprietary BricsCAD assemblies or third-party source/binaries that the project is not licensed to redistribute.
