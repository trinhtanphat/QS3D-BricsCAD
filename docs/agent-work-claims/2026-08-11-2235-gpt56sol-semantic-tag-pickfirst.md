# Work claim — Semantic Tag PICKFIRST

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-semantic-tag-pickfirst-20260811-2235`
- Registered: `2026-08-11T22:35:00+07:00`
- Baseline main SHA: `caccb67982d751ad0c827199a7d8a6bab6ec79cf`
- Priority: reduce repeated CAD picking in selection-first documentation authoring

## Reserved scope

Allow `QS3DTAG` and `QS3DTAGREFRESH` to consume one valid implied/PICKFIRST CAD source before falling back to the existing interactive entity picker, without weakening the existing input-before-bind lifecycle or source ownership validation.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/SemanticTagCommands.cs`
- one focused static preflight under `scripts/`
- one focused authoring note under `docs/`
- this claim file for close-out metadata

## Excluded scope

- No semantic-tag builder/content/handle ownership changes.
- No generated-object-as-source support; authoritative source-only policy remains unchanged.
- No placement/UCS semantics changes.
- No semantic tag removal/health/native cleanup changes.
- No Workspace/Ribbon redesign.
- No GitHub Actions dispatch or BricsCAD V25 runtime PASS claim.

## Defect evidence

Current `QS3DTAG` and `QS3DTAGREFRESH` are `CommandFlags.Modal` and always call `Editor.GetEntity(...)`, so a user who already selected the authoritative semantic source must select the same object again. This is unnecessary interaction because the existing source resolver already validates ownership fail-closed before native mutation.

## Validation plan

- Add `CommandFlags.UsePickSet` to both commands.
- Resolve exactly one implied selection handle first; zero implied selections fall back to the existing `GetEntity` prompt.
- Multiple implied selections fail closed before any canonical project bind instead of choosing arbitrarily.
- Preserve `QS3DTAG` placement completion before `ExistingProjectMutationContext.Require(...)`.
- Preserve `QS3DTAGREFRESH` selection completion before canonical bind.
- Add a static preflight locking these ordering and no-bootstrap constraints.

## Completion condition

Source change, static regression contract and focused documentation are merged into current `main`; claim is closed with exact SHAs and local-only runtime qualification remains explicit.