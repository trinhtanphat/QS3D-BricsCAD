# Work claim — Semantic Tag PICKFIRST

- Status: `COMPLETED`
- Agent: `chatgpt-gpt56sol-semantic-tag-pickfirst-20260811-2235`
- Registered: `2026-08-11T22:35:00+07:00`
- Completed: `2026-08-11T22:47:00+07:00`
- Baseline main SHA: `caccb67982d751ad0c827199a7d8a6bab6ec79cf`
- Merge PR: `#509`
- Merge SHA: `c825bb05ffc65acd0263e0df99239f02913db220`
- Priority: reduce repeated CAD picking in selection-first documentation authoring

## Reserved scope

Allow `QS3DTAG` and `QS3DTAGREFRESH` to consume one valid implied/PICKFIRST CAD source before falling back to the existing interactive entity picker, without weakening the existing input-before-bind lifecycle or source ownership validation.

## Completed source behavior

- Both commands now declare `CommandFlags.Modal | CommandFlags.UsePickSet`.
- Exactly one implied/PICKFIRST entity is consumed directly through `EntitySnapshotReader.ReadCurrentSelection(document)`.
- Zero implied selections preserve the existing explicit `Editor.GetEntity(...)` fallback.
- Multiple implied selections fail closed before canonical project binding instead of choosing an arbitrary source.
- `QS3DTAG` still completes source selection and placement before `ExistingProjectMutationContext.Require(...)` and revalidates the canonical source owner before native build.
- `QS3DTAGREFRESH` still completes source selection before canonical bind/native rebuild.
- Generated QS3D output remains invalid as Semantic Tag source; authoritative-source-only ownership is unchanged.
- No `GetOrCreate`/project bootstrap path was introduced.

## Added guard/docs

- `scripts/preflight-semantic-tag-pickfirst.py`
- `docs/SEMANTIC-TAG-PICKFIRST-2026-08-11.md`

The focused static gate is committed as a source contract. This connector-only lane did not dispatch GitHub Actions or claim a live BricsCAD V25 execution PASS.

## Excluded scope preserved

- No semantic-tag builder/content/handle ownership changes.
- No generated-object-as-source support.
- No placement/UCS semantics changes.
- No semantic tag removal/health/native cleanup changes.
- No Workspace/Ribbon redesign.

## Runtime qualification boundary

PICKFIRST, explicit picker fallback, multiple-selection fail-closed, ESC, active-DWG switching and native editor behavior still require real BricsCAD V25 local qualification. See `docs/SEMANTIC-TAG-PICKFIRST-2026-08-11.md` for the exact matrix.
