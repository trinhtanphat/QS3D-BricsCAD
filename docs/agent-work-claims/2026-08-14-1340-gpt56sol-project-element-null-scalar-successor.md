# Work claim — ProjectElement null persisted-scalar successor

- Status: `COMPLETED`
- Agent: `gpt56sol-project-element-null-scalar-successor-20260814-1340`
- Registered: `2026-08-14T13:40:00+07:00`
- Completed: `2026-08-14T13:43:00+07:00`
- Predecessor: `docs/agent-work-claims/2026-08-14-1334-gpt56sol-project-element-null-scalar-persistability.md` (`RELEASED` after collision)
- Priority: `P1 / persistence-integrity`

## Coordination

The earlier `ProjectElement id persistability` owner completed as `847f1f7992e41430e080b9972a92e17c75145a65`. This successor was then claimed against the refreshed source and preserves the new `RequireId(...)` validation unchanged.

## Implemented correction

- `67078ff3e5caebc9abee27fe9639bae794b1c890` — `fix(core): canonicalize null ProjectElement scalars`
  - adds non-null backing storage for `FamilyId`, `FloorId`, `ZoneId`, and `DrawingFingerprint`;
  - runtime null assignments canonicalize immediately to `string.Empty`;
  - constructor relation trimming and exact non-null setter values remain unchanged;
  - no dirty/version/reference/generated-output semantics were added to these setters.
- `7defd21b02a5b3712e3e6706f30f4871aedbf231` — `test(core): guard null ProjectElement scalar persistability`
  - covers all four null assignments;
  - preserves constructor trim behavior and exact non-null setter semantics;
  - covers QSDB `SaveNew` -> `Load` canonical-empty round-trip.

## Validation

- Live `main` read-back confirms the four backing fields/setters and the pre-existing `RequireId` path coexist in current source.
- Live `main` read-back confirms the self-registering smoke source is present.
- Executable Core smoke: `NOT_RUN` in this connector-only lane.
- GitHub Actions: `NOT_DISPATCHED` by this lane.
- BricsCAD runtime: `NOT_RUN` / not applicable to this Core persistence correction.

## Non-scope preserved

- no relation existence/category validation;
- no dirty/UpdatedUtc mutation from these setters;
- no generated-output invalidation;
- no SourceHandles/DependsOn/property/quantity changes;
- no QSDB schema/migration/UI/native changes.
