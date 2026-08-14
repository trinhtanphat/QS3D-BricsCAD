# Work claim — ProjectElement null persisted-scalar successor

- Status: `ACTIVE`
- Agent: `gpt56sol-project-element-null-scalar-successor-20260814-1340`
- Registered: `2026-08-14T13:40:00+07:00`
- Predecessor: `docs/agent-work-claims/2026-08-14-1334-gpt56sol-project-element-null-scalar-persistability.md` (`RELEASED` after collision)
- Priority: `P1 / persistence-integrity`

## Coordination

The earlier `ProjectElement id persistability` owner has now completed (`847f1f7992e41430e080b9972a92e17c75145a65`). Current source keeps its new `RequireId(...)` validation. No newer ProjectElement claim is visible in the refreshed commit index. This successor reserves the remaining null-scalar representation gap without altering the completed ID work.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectElement.cs` — only non-null backing storage for `FamilyId`, `FloorId`, `ZoneId`, `DrawingFingerprint`.
- new `tests/QS3D.Core.SmokeTests/ProjectElementNullScalarPersistabilitySmoke.cs`.
- this claim file.

## Contract

Runtime null assignments become `string.Empty` immediately. Preserve constructor relation trimming, the newly landed `RequireId` behavior, every non-null setter value exactly, and existing dirty/reference/generated-output semantics. Add SaveNew -> Load regression coverage. No schema, UI, native, Actions or local-runtime changes.

## Validation boundary

Read back live `main` source/test after landing. Executable smoke and runtime remain `NOT_RUN` unless independently executed.
