# Agent work claim — ProjectState name persistability

Status: `ACTIVE`

Agent: `chatgpt-web-gpt56sol-project-name-persistability-20260814-1258`

Registered: `2026-08-14T12:58:06+07:00`

Baseline `main`: `1d0ab71be6242e50e4a2d0607bad7545af44fe65`

Priority: `P1` Core persistence-integrity hardening.

## Confirmed defect

`ProjectState.Name` is canonical persisted semantic state written by QSDB as the root `name` XML attribute. The public constructor accepts any non-whitespace name after `Trim()`, and the public setter routes through `RequireProjectName`, which currently only rejects blank text and trims surrounding whitespace.

A non-blank name containing an embedded control character such as `"Project\u0001Name"` therefore succeeds through the supported writer boundary and only fails later when `QsdbProjectStore.ValidateSerializedXmlText`/XML serialization verifies XML characters. The writer can create a project state that cannot be persisted normally.

This lane makes the project-name writer fail before semantic mutation. It is intentionally separate from ProjectElement quantity/property work and from the completed ProjectState scalar versioning lanes.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectState.cs` only the project `Name` constructor/setter validation path.
- `tests/QS3D.Core.SmokeTests/ProjectNameFreshnessSmoke.cs`.
- this claim file.

## Acceptance

1. Canonical project names and surrounding-whitespace normalization keep existing behavior.
2. The constructor's existing blank/whitespace fallback to `"QS3D Project"` remains unchanged.
3. Embedded control characters in a non-blank project name are rejected by both constructor and setter before the invalid value becomes project state.
4. A rejected setter call preserves `Name`, `ChangeVersion`, `UpdatedUtc`, and persistence-stamp cleanliness.
5. Canonical-equivalent setter no-op, real rename exact-once revision, snapshot restore, and QSDB format remain unchanged.

## Explicit non-scope

- No `ProjectId`, DrawingPath/Fingerprint, ActiveZone/ActiveFloor, Floor/Zone/Family name changes.
- No ProjectElement `SetProperty`/`SetQuantity` changes; active quantity claim owns `ProjectElement.cs`.
- No QSDB schema/migration or serializer-format change.
- No mapping, interchange, report/UI, BricsCAD/native changes.

## Evidence / history

- Existing `ProjectNameFreshnessSmoke` from `503edb6e2bdee487c5d45f3849fa4e5ad5582f6f` establishes trim/no-op/version/snapshot semantics but does not cover control-character persistability.
- Current QSDB save path explicitly validates serialized XML characters and rejects invalid XML text after the project writer has already accepted it.
- No current commit-history claim was found for project-name control-character/persistability hardening.
- ACTIVE quantity mutation claim `docs/agent-work-claims/2026-08-14-1253-gpt56sol-quantity-mutation-persistability.md` reserves `ProjectElement.cs`; this lane does not touch it.

## Validation plan

- Extend existing focused ProjectName freshness smoke with constructor/setter control-character rejection and atomicity assertions.
- Re-read source/test from remote current `main` after writes.
- GitHub Actions: `NOT_RUN` / do not dispatch.
- .NET Core smoke execution: `NOT_RUN` because this environment has no `dotnet` executable.
- BricsCAD/native runtime: `NOT_RUN`; no native PASS claim.

## Completion condition

Claim-only reservation is on remote `main`; source + focused regression are reconciled against live `main`; remote readback verifies final contents; then status is changed to `COMPLETED` with exact on-main commit SHAs and validation limitations.
