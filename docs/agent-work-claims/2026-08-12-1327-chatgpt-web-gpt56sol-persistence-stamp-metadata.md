# Work claim — Persistence stamp metadata freshness

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-persistence-stamp-metadata`
- Registered: `2026-08-12T13:27:00+07:00`
- Baseline main SHA: `2764aa2ba9cf79d8248da908edeffb35936cb128`
- Priority: P1 — persisted project metadata must not change while the save stamp remains false-clean.

## Confirmed defect

`ProjectPersistenceStamp` currently treats `ProjectState.ChangeVersion` as the general save-freshness signal and special-cases only `QS3D.RecoveredFromBackup`. `ProjectState.Metadata` is an exposed mutable `IDictionary<string,string>` that is persisted to QSDB, but direct metadata mutation does not call `ProjectState.Touch()`.

Concrete counterexample: create a project and persistence stamp, then set `project.Metadata["Custom.Persistence.Flag"] = "enabled"`. The project now contains different persisted state while `ChangeVersion` is unchanged, `QS3D.RecoveredFromBackup` is absent, and `RequiresSave(project)` incorrectly returns `false`. A subsequent close path that trusts the stamp can therefore treat unsaved metadata as clean.

The completed persistence-stamp instance-identity lane is preserved; this claim is only about metadata content freshness for the owning `ProjectState` instance.

## Reserved scope

- `src/QS3D.Core/Persistence/ProjectPersistenceStamp.cs`, limited to metadata freshness tracking
- focused Core smoke regression under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Intended contract

- A metadata key add, value change, or removal after stamp creation / `MarkSaved()` makes `RequiresSave()` return `true`, even when `ChangeVersion` is unchanged.
- Metadata comparison follows the project's case-insensitive key semantics and preserves exact value semantics.
- `MarkSaved()` refreshes both the saved change revision and saved metadata snapshot.
- Preserve the existing `QS3D.RecoveredFromBackup=true` forced-save contract.
- Preserve project-instance ownership checks and do not alter QSDB schema, `ProjectState.Metadata` API shape, or unrelated mutation services.

## Validation boundary

No GitHub Actions or BricsCAD runtime/build PASS will be claimed unless actually observed.