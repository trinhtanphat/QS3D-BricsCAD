# Agent Work Claim — Missing dependency health diagnostic

- Agent: ChatGPT Web / GPT-5.6 Sol
- Date: 2026-08-11
- Status: ACTIVE
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

`DependencyHealthService` currently records self-references and references to duplicate/ambiguous semantic IDs, but silently drops a non-empty dependency ID that does not exist in the project. Current `QS3DBUILD3D` dependency-scoped regeneration explicitly fails closed when such a dependency is missing and tells the user to run Health/repair dependencies (`102d47ba567a776b4a2c642a09fc91d693fe9da0`). The Health service therefore can omit the exact blocker that native rebuild now requires Health to expose.

## Plan

1. During dependency graph construction, capture non-empty dependency IDs that are neither duplicate/ambiguous nor uniquely present in the project.
2. Emit deterministic `DEPENDENCY_TARGET_MISSING` Error issues keyed to the referencing element, with the missing semantic ID in the message.
3. Preserve existing duplicate-target, self-reference and cycle behavior/order.
4. Add a Core smoke proving missing targets are surfaced, repeated equivalent dependency tokens are de-duplicated per element, and a valid existing dependency does not create a false positive.
5. Register the smoke from the latest shared registry blob.
6. Re-read current `main`, inspect status evidence, and close the claim with exact SHAs. Do not claim runtime/CI unless actually executed.
