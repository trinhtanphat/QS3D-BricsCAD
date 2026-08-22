# Agent work claim — ProjectState name persistability

Status: `COMPLETED`

Agent: `chatgpt-web-gpt56sol-project-name-persistability-20260814-1258`

Registered: `2026-08-14T12:58:06+07:00`

Completed: `2026-08-14T13:01:32+07:00`

Baseline `main`: `1d0ab71be6242e50e4a2d0607bad7545af44fe65`

Priority: `P1` Core persistence-integrity hardening.

## Confirmed defect

`ProjectState.Name` is canonical persisted semantic state written by QSDB as the root `name` XML attribute. The public constructor accepted any non-whitespace name after `Trim()`, and the public setter routed through `RequireProjectName`, which only rejected blank text and trimmed surrounding whitespace.

A non-blank name containing an embedded control character such as `"Project\u0001Name"` therefore succeeded through the supported writer boundary and only failed later when `QsdbProjectStore.ValidateSerializedXmlText`/XML serialization verified XML characters. The writer could create a project state that could not be persisted normally.

## Implemented

- The constructor retains the existing blank/whitespace fallback to `"QS3D Project"`, but non-blank names now use the same `RequireProjectName` path as the setter.
- `RequireProjectName` still rejects blank values and trims surrounding whitespace, then rejects any embedded control character before returning the canonical name.
- The setter still validates before revision calculation/mutation, so rejected names preserve the previous name, timestamp and semantic version.
- No other ProjectState scalar/name domain was changed.

## Acceptance result

1. Canonical project names and surrounding-whitespace normalization are preserved.
2. Blank constructor fallback remains `"QS3D Project"`.
3. Embedded control characters are rejected by constructor and setter before invalid state is committed.
4. Focused smoke source asserts a rejected setter preserves `Name`, `ChangeVersion`, `UpdatedUtc`, and persistence-stamp cleanliness.
5. Existing canonical-equivalent no-op, real rename revision, snapshot restore and concurrent overflow atomicity regression remain intact.

## Commits on `main`

- Claim: `efb2f5c1090ea17efa3fcd86c3492fd5de8e9e90`
- Source: `941c768c33dcdd3f6a46cabe99a29582f807be1a`
- Focused regression: `babe450f826d74922239dd15c44306d9a0af6067`

## Concurrent reconciliation

The live `ProjectNameFreshnessSmoke.cs` had gained an independent project-name overflow atomicity regression after the earlier historical snapshot. That coverage was preserved verbatim while adding constructor fallback/control-character and setter atomicity assertions. No concurrent source or test work was overwritten.

The ACTIVE quantity mutation claim `docs/agent-work-claims/2026-08-14-1253-gpt56sol-quantity-mutation-persistability.md` owns `ProjectElement.cs`; this lane did not touch that file or its quantity scope.

## Verification

- Remote `main` was re-read at `babe450f826d74922239dd15c44306d9a0af6067` after the regression commit.
- Final `ProjectState.cs` was verified to route non-blank constructor names through `RequireProjectName` and reject `normalized.Any(char.IsControl)`.
- Final smoke source was verified to retain the pre-existing overflow atomicity regression and add `\u0001` constructor/setter rejection plus post-failure name/version/timestamp/persistence-stamp assertions.
- GitHub Actions: `NOT_RUN` / not dispatched.
- .NET Core smoke execution: `NOT_RUN` because this environment has no `dotnet` executable.
- BricsCAD/native runtime: `NOT_RUN`; no native PASS claimed.
- Force push: not used.

## Explicit non-scope retained

- No `ProjectId`, DrawingPath/Fingerprint, ActiveZone/ActiveFloor, Floor/Zone/Family name changes.
- No ProjectElement `SetProperty`/`SetQuantity` changes.
- No QSDB schema/migration or serializer-format change.
- No mapping, interchange, report/UI, BricsCAD/native changes.
