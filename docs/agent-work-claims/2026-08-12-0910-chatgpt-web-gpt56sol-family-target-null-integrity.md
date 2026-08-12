# Work claim — Family target operations null collection integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-family-target-null-integrity-20260812-0910`
- Registered: `2026-08-12T09:10:00+07:00`
- Completed: `2026-08-12T09:13:00+07:00`
- Baseline main SHA: `55301299f8878eee87ef447aa110bb98cd01af73`
- Priority: P1 — Family target operations must fail closed when the project Family collection itself is structurally invalid.

## Completed scope

`ProjectFamilyService` target-based operations now reject any `null` entry in `project.Families` before resolving or using a target Family. The completed global-duplicate helper no longer silently skips malformed null entries, aligning target operations with the existing Create structural-null contract.

## Pushed implementation

- Claim registration: `4fba2a8fd11c74e5cb79b416ba7c4142ec229fb2`
- Source fix: `eb752d4305e91be94ce1011be3ec055a8ec170dc`
- Focused Core smoke: `84553f6bd91e1153684b643c9fff7505d27a8325`

## Validation evidence

- Readback from current `main` confirms `ValidateUniqueFamilyIds(project)` throws `InvalidOperationException("Project family collection contains a null family.")` instead of continuing past a null Family entry.
- `ProjectFamilyGlobalNullIntegritySmoke` covers `Duplicate`, `Rename`, `SetProperty`, `RemoveProperty`, `Assign`, `Delete`, and `ReferenceCount` against a valid target plus unrelated null Family state.
- The smoke snapshots Family count/name/property, element FamilyId, `ChangeVersion`, and `UpdatedUtc` across rejected operations, and includes valid rename/assign/reference-count controls.
- Connector ancestry check confirmed source commit `eb752d4305e91be94ce1011be3ec055a8ec170dc` remained an ancestor of moving `main`; subsequent concurrent files were outside this reserved scope.
- Test source was read back from current `main` after push.

## Excluded / remaining validation

- Family Create null/duplicate lanes and Family assignment null-element lane remain separate completed work.
- Family activation/UI state, FamilyWindow, audit/no-op behavior, templates, persistence/interchange, Floor/Zone services, and native BricsCAD adapters were not changed.
- GitHub Actions were not dispatched because the owner request was `continue all fix bug update code`, which is not CI authorization under `CI_POLICY.md`.
- No local compile, executable smoke run, or licensed BricsCAD V25/V26 runtime PASS is claimed from this web session.

## Completion condition

`COMPLETED`: target Family operations fail closed on null Family collection entries, focused deterministic Core smoke coverage is present on `main`, ownership is released, and no concurrent work was overwritten.
