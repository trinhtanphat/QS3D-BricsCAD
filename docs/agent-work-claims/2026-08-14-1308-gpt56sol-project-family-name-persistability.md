# Work claim — ProjectFamily name persistability

- Status: `ACTIVE`
- Agent: `gpt56sol-project-family-name-persistability-20260814-1308`
- Registered: `2026-08-14T13:08:00+07:00`
- Workstream: `CORE / persistence-integrity`
- Priority: `P1`
- Baseline: `3ccdc8d055d0be07c16e7afa208b8b94fd9665dd`

## Confirmed defect

`ProjectFamily.Name` is a persisted mutable domain field. Both the public constructor and public `Name` setter use `RequireName`, which currently rejects only blank input and trims surrounding whitespace. An embedded XML-invalid control character such as `U+0001` is therefore accepted as a family name.

QSDB serializes family names directly to XML attributes and validates XML characters before publication. A supported constructor or rename can therefore create a family state that fails only when persisted. This is a narrow writer-boundary persistability defect independent of the already-completed ProjectFamily id lane.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectState.cs` — only `ProjectFamily.RequireName` behavior.
- new `tests/QS3D.Core.SmokeTests/ProjectFamilyNamePersistabilitySmoke.cs`.
- this claim file.

## Intended change

Preserve blank rejection and surrounding-whitespace normalization, but reject control characters in the normalized family name before constructor assignment or mutable rename. Preserve case semantics, category/id behavior, PropertyChanged semantics for valid renames and all family property/assignment services.

## Regression plan

Focused self-registering smoke will prove:

1. canonical and padded names remain supported;
2. constructor rejects a `U+0001` name;
3. setter rejects a `U+0001` rename before mutation;
4. failed rename preserves prior name and emits no `PropertyChanged` event.

## Explicit non-scope

- no ProjectFamily id changes (completed separately);
- no family category/property-map/assignment changes;
- no Zone/Floor/Project names or ids;
- no QSDB schema/migration changes;
- no mapping/export/UI/native changes;
- no GitHub Actions or licensed BricsCAD qualification.

## Validation boundary

GitHub connector read/write is available, but this environment has no local checkout/.NET/native runner. Executable PASS will not be claimed without independent evidence. Completion requires remote diff/readback and ancestry verification on current `main`.
