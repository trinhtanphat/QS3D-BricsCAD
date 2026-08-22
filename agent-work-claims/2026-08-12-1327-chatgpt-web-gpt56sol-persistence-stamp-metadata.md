# Work claim — Persistence stamp metadata freshness

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-persistence-stamp-metadata`
- Registered: `2026-08-12T13:27:00+07:00`
- Baseline main SHA: `2764aa2ba9cf79d8248da908edeffb35936cb128`
- Priority: P1 — persisted project metadata must not change while the save stamp remains false-clean.

## Confirmed defect

`ProjectPersistenceStamp` treated `ProjectState.ChangeVersion` as the general save-freshness signal and special-cased only `QS3D.RecoveredFromBackup`. `ProjectState.Metadata` is an exposed mutable `IDictionary<string,string>` that is persisted to QSDB, but direct metadata mutation does not call `ProjectState.Touch()`.

Concrete counterexample: create a project and persistence stamp, then set `project.Metadata["Custom.Persistence.Flag"] = "enabled"`. The project now contains different persisted state while `ChangeVersion` is unchanged, `QS3D.RecoveredFromBackup` is absent, and the old `RequiresSave(project)` returned `false`.

The completed persistence-stamp instance-identity lane is preserved; this claim is only about metadata content freshness for the owning `ProjectState` instance.

## Implemented contract

- A metadata key add, value change, or removal after stamp creation / `MarkSaved()` makes `RequiresSave()` return `true`, even when `ChangeVersion` is unchanged.
- Metadata comparison follows the project's case-insensitive key semantics and preserves exact value semantics.
- `MarkSaved()` refreshes both the saved change revision and saved metadata snapshot.
- The existing `QS3D.RecoveredFromBackup=true` forced-save contract is preserved.
- Project-instance ownership checks, QSDB schema, `ProjectState.Metadata` API shape, and unrelated mutation services are unchanged.

## Commits

- Claim: `43b21e843fc3426d71e7930e031016f20e0ddb47`
- Source fix: `a115092d99c7f610fccfaa87d3b5d7c318716f28`
- Regression smoke: `9c938c1d7846fa3c2a3ba3b6b8f5ad0a543d9349`

## Validation

Read-back from `main` confirmed the source metadata snapshot/comparison and the focused smoke regression are present after concurrent repository writes. No GitHub Actions were dispatched and no BricsCAD runtime/build PASS is claimed.