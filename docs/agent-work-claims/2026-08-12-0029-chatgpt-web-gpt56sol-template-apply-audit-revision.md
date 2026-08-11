# Work claim — Template apply audit-owned project revision

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-template-apply-audit-revision`
- Registered: `2026-08-12T00:29:00+07:00`
- Baseline main SHA: `b5579e12bf871a5c01f9316fdcb5a28a56f1acdc`
- Priority: P1 — one audited template apply must advance project revision exactly once.

## Confirmed defect

`TemplateProfileStore.Apply(...)` performs the template mutations, then calls `project.Touch()`, immediately followed by `AuditTrail.ForProject(project).Record("template.apply", ...)`. A project-bound audit record already advances `ProjectState.ChangeVersion`, so one logical audited template application advances the project revision twice.

This is the same established defect class already fixed in HostLink, semantic tag/table, grid annotation and other audited mutation paths: the audit record is the project revision owner when it is part of the rollback-protected semantic operation.

## Reserved scope

- `src/QS3D.Core/Templates/TemplateProfileStore.cs`
- `tests/QS3D.Core.SmokeTests/TemplateApplyAuditRevisionSmoke.cs` (new auto-registered focused smoke)
- this claim file

## Intended contract

- A successful template apply appends one `template.apply` audit event and advances `ChangeVersion` exactly once.
- Template mutation results, family/rule propagation, dirty flags, audit detail, rollback semantics and persistence remain unchanged.
- No changes to the current `QS3DTEMPLATEIMPORT` freshness/UI lane; that active claim reserves `TemplateCommands.cs` only.

## Validation plan

- Apply a small in-memory profile that adds one family and assert one audit event + `ChangeVersion == before + 1`.
- Preserve returned result counts and family content.
- Re-fetch the source immediately before update, SHA-guard the write, inspect exact diff, then close this claim.
- No GitHub Actions dispatch; no executable .NET or BricsCAD V25 runtime PASS claim from this hosted environment.

## Completion condition

Template apply has one audit-owned project revision boundary, focused regression is on `main`, and this claim is closed.
