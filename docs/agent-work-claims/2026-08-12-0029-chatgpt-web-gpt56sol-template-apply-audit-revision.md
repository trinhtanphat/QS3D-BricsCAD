# Work claim — Template apply audit-owned project revision

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-template-apply-audit-revision`
- Registered: `2026-08-12T00:29:00+07:00`
- Completed: `2026-08-12T00:31:00+07:00`
- Baseline main SHA: `b5579e12bf871a5c01f9316fdcb5a28a56f1acdc`
- Reservation commit: `dc86668c28db5a554b8436867f550fd249db452b`
- Priority: P1 — one audited template apply must advance project revision exactly once.

## Defect fixed

`TemplateProfileStore.Apply(...)` performed the template mutations, then called `project.Touch()`, immediately followed by `AuditTrail.ForProject(project).Record("template.apply", ...)`. A project-bound audit record already advances `ProjectState.ChangeVersion`, so one logical audited template application advanced the project revision twice.

The redundant explicit touch has been removed. The existing audit record remains inside the rollback-protected apply scope and is now the single project revision owner.

## Reserved scope

- `src/QS3D.Core/Templates/TemplateProfileStore.cs`
- `tests/QS3D.Core.SmokeTests/TemplateApplyAuditRevisionSmoke.cs`
- this claim file

## Published commits

- `2c470641772c04bf7c8911a5fac7d699fdbfb7d7` — remove the redundant `project.Touch()` immediately before the project-bound template audit record.
- `16c643eb23fa470832541c21e210235562f80736` — add isolated auto-registered smoke proving one changed template apply yields one audit event and one project revision while preserving result/family content.

## Delivered contract

- A successful template apply appends one `template.apply` audit event and advances `ChangeVersion` exactly once.
- Template mutation results, family/rule propagation, dirty flags, audit detail, rollback semantics and persistence remain unchanged.
- The concurrent `QS3DTEMPLATEIMPORT` freshness/UI lane remains untouched; it reserves `TemplateCommands.cs` only.

## Validation notes

- Exact post-publication source diff contains one deletion only: the redundant explicit project touch.
- Exact regression diff is isolated in a new `ModuleInitializer` smoke; shared smoke registration was not edited.
- No force-push and no GitHub Actions dispatch.
- This hosted environment does not provide the repository .NET/BricsCAD V25 qualification toolchain, so executable/native runtime PASS is not claimed.

## Completion condition

Satisfied for the remote-safe source/static contract. Exact executable/native qualification remains separate.
