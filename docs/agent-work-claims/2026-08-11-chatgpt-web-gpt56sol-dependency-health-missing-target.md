# Agent Work Claim — Missing dependency health diagnostic

- Agent: ChatGPT Web / GPT-5.6 Sol
- Date: 2026-08-11
- Status: COMPLETE
- Branch/target: direct `main` under current `AGENTS.md` coordination policy

## Scope reservation

- `src/QS3D.Core/Diagnostics/DependencyHealthService.cs`
- `tests/QS3D.Core.SmokeTests/DependencyHealthMissingTargetSmoke.cs` (new)
- `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs` (shared registry; re-read immediately before update)
- this claim file

## Explicit exclusions

- No Model Health WPF/review UI; the active Model Health review claim explicitly excludes Core Diagnostics service logic.
- No Build3D command/regeneration behavior changes.
- No ProjectState mutation/persistence changes owned by the active Core mutation-atomicity lane.
- No generated ownership, Rebar, Room, Reporting, Interchange or local-only runtime surfaces.

## Verified defect

`DependencyHealthService` recorded self-references and references to duplicate/ambiguous semantic IDs, but silently dropped a non-empty dependency ID that did not exist in the project. Current `QS3DBUILD3D` dependency-scoped regeneration explicitly fails closed when such a dependency is missing and tells the user to run Health/repair dependencies (`102d47ba567a776b4a2c642a09fc91d693fe9da0`). Health could therefore omit the exact blocker native rebuild expected it to expose.

## Completed implementation

- `dab7a03c2a4dced4d68bad53357508ddf22ae4e8` — registered this work claim before source changes.
- `654210559a9e330509ea3c87134bdc1e4a2b4ad0` — `DependencyHealthService` now emits deterministic `DEPENDENCY_TARGET_MISSING` Error issues for non-empty dependency IDs that are neither ambiguous nor present in the project. Existing ambiguous-target, self-reference and cycle behavior remains unchanged.
- `c5d769b05fa3bda5fb83ea33ef09f445132789d5` — added `DependencyHealthMissingTargetSmoke`, covering missing-target reporting, case-insensitive duplicate dependency-token de-duplication, Error severity/source element identity, and no false positive for a valid dependency.
- `15a5b3627b56387862bb30535e7c1e9c127e10b6` — registered the new smoke from the latest shared registry blob while preserving concurrent registrations.

## Validation actually performed

- Re-read current source and inspected the implementation diff; only the missing-target diagnostic path was added.
- Re-read the shared smoke registry before updating it.
- GitHub combined status for registry commit `15a5b3627b56387862bb30535e7c1e9c127e10b6` returned no automatic statuses/checks.
- No `LOCAL_PASS`, BricsCAD V25 runtime PASS, or CI PASS is claimed. Earlier in this session the container could not clone GitHub because DNS resolution for `github.com` failed, so runtime smoke execution was unavailable here.

## Completion

The source-proven missing-dependency Health gap is fixed on `main` with focused regression coverage and this claim is released.