# Work claim — ProjectFamily id persistability

- Status: `ACTIVE`
- Agent: `gpt56sol-project-family-id-persistability-20260814-1304`
- Registered: `2026-08-14T13:04:00+07:00`
- Workstream: `CORE / persistence-integrity`
- Priority: `P1`
- Baseline: `90198b228f24c9f26fc1f0c57600f7750655ea57`
- Pre-write source blob: `25e761658c848ea94f589aa3f528d4cdbf041304`

## Confirmed defect

`ProjectFamily` is the persisted Core family model. Its public constructor validates `id` only for blank input and surrounding whitespace, then exposes the normalized id as immutable relational identity. An id containing an embedded XML-invalid control character such as `U+0001` is therefore accepted by the supported domain boundary.

QSDB serializes `ProjectFamily.Id` directly into family XML attributes and runs `XmlConvert.VerifyXmlChars` before publication. The accepted family is consequently rejected only at persistence time, after callers have already constructed and may have referenced the family. This is a writer/constructor persistability gap, not direct collection corruption and not a Family-assignment semantic change.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectState.cs` — only `ProjectFamily` id validation.
- new `tests/QS3D.Core.SmokeTests/ProjectFamilyIdPersistabilitySmoke.cs`.
- this claim file.

## Intended change

Normalize the family id exactly as today, then reject embedded control characters before assigning immutable `Id`. Preserve blank rejection, surrounding-whitespace normalization, case semantics, name/category behavior, property-map behavior and all assignment services.

## Regression plan

Add focused self-registering Core smoke proving:

1. canonical family ids remain accepted;
2. padded ids still normalize to the canonical identity;
3. an embedded `U+0001` id throws `ArgumentException` at construction;
4. no ProjectState/Family assignment, family-name, property-map, Zone/Floor or QSDB behavior is otherwise changed.

## Explicit non-scope

- no `ProjectFamily.Name` changes;
- no Zone/Floor/Project id or name changes;
- no Family property-map changes;
- no Family assign/activate/delete/rename service changes;
- no QSDB schema/migration changes;
- no mapping/export/UI/native changes;
- no GitHub Actions or licensed BricsCAD qualification.

## Validation boundary

This environment has GitHub connector read/write but no local checkout/.NET/native runner. Source/regression commits may be published only after live-main reconciliation and remote readback. Executable PASS will not be claimed unless independently evidenced on the resulting SHA.

## Completion condition

Claim-only reservation is visible on remote `main`; source + focused regression are reconciled against current `main`; remote readback and ancestry verify both changes; then this claim is closed `COMPLETED` with exact SHAs and validation limitations.
