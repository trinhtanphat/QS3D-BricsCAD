# QS3D health and preflight contracts

## Model health

`QS3DHEALTHALL` is the broad model-review entry point. Its exact service set evolves with source, but the contract is stable: inspect semantic/project/source integrity, dependency health, generated ownership/freshness and supported generated families without hiding invalid state.

`QS3DRELEASECHECK` is stricter. It adds release-readiness/liveness/BOM/runtime-facing guards and must not be weakened simply to make incomplete project data appear green.

Generated freshness is **not** equivalent to `element.Dirty != None`. Geometry/Properties/Relations edits can stale generated output; quantity-only changes do not necessarily do so. Health code should use the canonical `ProjectElement.IsGenerated...Stale()` and generated-owner APIs instead of feature-local handle lists.

## Repository preflight layers

### Generic guard — `scripts/preflight.py`

The generic guard owns repository-wide source policy and cross-cutting invariants. Among other checks it:

- validates required source/project files and XML/XAML parseability;
- protects the clean-room boundary and approved synthetic CAD fixtures;
- rejects committable `.dwg`, `.dxf` and `.docx` private/reference artifacts **case-insensitively**, so the result is consistent on Windows and POSIX runners;
- enforces manual-only GitHub Actions policy for both `.yml` and `.yaml` workflow files;
- checks representative persistence, ownership, lifecycle, UI wiring and release-health invariants.

The guard itself must remain valid Python. A syntax failure in repository tooling is a repository-health failure, not a feature-specific failure.

### Repository-health regression — `scripts/preflight-repository-health.py`

This gate parses every Python file under `scripts/` with `ast.parse` and protects the generic cross-platform artifact/workflow checks. Its purpose is to catch broken repository tooling before a feature gate can be trusted.

The repository-health gate is fail-closed around its own coordination dependencies: `scripts/preflight.py` and `AGENTS.md` must exist. It also reads the `AGENTS.md` **Mandatory handoff reading order**, extracts repository-relative Markdown paths from that section and verifies that every referenced file exists. This prevents a documentation rename/removal from silently leaving new agents with a broken mandatory startup path.

Because its filename matches `preflight-*.py`, it is automatically discovered by the aggregate runner.

### Aggregate runner — `scripts/preflight-all.py`

The aggregate runner discovers every `scripts/preflight-*.py` gate except itself, executes gates in deterministic filename order, applies a per-gate timeout and reports all failed gates before returning non-zero.

`scripts/preflight.py` is intentionally run separately as the generic source guard; CI then runs the aggregate feature/repository-health gates.

### Package hash-manifest integrity — `scripts/preflight-package-hash-manifest-coverage.py`

Release package producers hash every regular package file except `SHA256SUMS.txt`. The installer mirrors that contract at the final mutation boundary: manifest names are case-insensitively unique and the manifest set must exactly equal the recursively enumerated regular package-file set (again excluding only the manifest itself). An unlisted file, stale manifest-only entry or case-colliding duplicate therefore fails before payload copy or DemandLoad registration.

The secure updater keeps a separate outer boundary: it verifies the SHA-256 of the complete downloaded ZIP before extraction, validates archive safety, and only then delegates installation to the packaged installer. The package-integrity regression protects this producer → whole-ZIP hash → exact internal manifest coverage chain without duplicating the installer algorithm inside the updater.

## Command/UI wiring

`scripts/preflight-command-wiring.py` collects QS3D `CommandMethod` registrations and checks command references from XAML buttons, Ribbon specs and simple UI dispatch paths. UI/Ribbon references must resolve to registered commands so multi-agent rename races do not become BricsCAD `Unknown command` failures.

Other feature preflights protect product-boundary, Direct Draw and additional source contracts. Adding a new `preflight-<feature>.py` automatically places it under the aggregate runner; it does not authorize any workflow dispatch.

## CI policy

GitHub Actions workflows remain `workflow_dispatch` only unless [`../CI_POLICY.md`](../CI_POLICY.md) is explicitly changed. A commit, push, documentation update, review, handoff or `continue all` request does **not** authorize running a manual workflow.

`scripts/preflight-ci-manual-only.py` treats the job-level event condition as a semantic safety boundary rather than a substring check. Every executable job must use `github.event_name == 'workflow_dispatch'` as the leading conjunction; YAML comments, negated equality and `||` bypass branches cannot satisfy the guard. Both `release-v25.yml` and `release-v25-cloud.yml` additionally require the canonical `inputs.confirm_release == 'RELEASE'` conjunction on their `release` job. The parser carries deterministic positive/negative regression cases so comment-only or bypassing expressions fail closed.

A manually approved validation should run the generic/source guards before relevant Core/V25 build, smoke or runtime stages.

## What static gates do not prove

Static preflight can prove source wiring, repository policy and regression registration. Core smoke tests can prove deterministic code that does not require BricsCAD. Neither proves, by itself:

- exact V25 `BrxMgd.dll` / `TD_Mgd.dll` compatibility for the newest SHA;
- licensed `NETLOAD` / DemandLoad behavior;
- native `Solid3d` authoring/boolean robustness on representative private DWGs;
- modeless multi-DWG lifecycle under the real BricsCAD host;
- Direct Draw editor interaction/cancellation/rollback on the real host;
- Ribbon/WPF/HiDPI visual behavior;
- signed package/update rollback behavior;
- large-project performance.

Those remain local BricsCAD V25 qualification gates and must not be reported as passed without evidence for the exact candidate SHA.
