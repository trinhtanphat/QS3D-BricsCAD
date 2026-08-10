# QS3D for BricsCAD V25

Clean-room BricsCAD V25 quantity takeoff / 3D QS plugin inspired by the workflow shown in the supplied BLT3D references. This repository does **not** contain BLT source, BLT binaries, BricsCAD proprietary assemblies, or private drawings.

## Target
- BricsCAD V25 on Windows x64
- Plugin: C# / .NET Framework 4.8 / WPF / BricsCAD .NET API
- Core engine: `netstandard2.0`
- UI: native BricsCAD viewport + QS3D ribbon/palettes
- Project source of truth: DWG geometry + `.qsdb` semantic metadata

## Commands
- `QS3D` — show QS3D workspace
- `QS3DHIDE` — hide QS3D palettes
- `QS3DINSPECT` — inspect current/prompted selection
- `QS3DBQ` — quantity summary + Excel export
- `QS3DHEALTH` — model health diagnostics
- `QS3DABOUT` — build identity

## V1 architecture
- Project/zone/floor/family/element model
- dependency graph + dirty regeneration
- deterministic quantity rules
- model health/orphan diagnostics
- bulk edit, revision snapshots and feature flags
- WPF design system and data-driven property inspector
- live Layer/Xref adapters after V25 runtime verification
- Tường KT / HT_Phòng / Cửa / BQ / Excel workflows
- atomic project save/backup and single-writer project locking

## Build policy
Do not commit `BrxMgd.dll`, `TD_Mgd.dll`, BLT/BLT3D folders, or private DWG/DOCX fixtures. The BricsCAD plugin resolves V25 assemblies through `BRICSCAD_V25_DIR` with `Private=false`.

GitHub Actions on `main` are **manual-only and owner-controlled**. Documentation/Markdown, `docs:` and `chore:` commits do not need GitHub CI, and no commit/push/merge should dispatch Actions automatically. Even source changes run GitHub CI only when the repository owner explicitly requests it.

This is a multi-agent repository. Agents must sync the latest `main` before work and again before commit/push so concurrent changes are not overwritten. Agents with real local-machine access should prioritize tasks that require BricsCAD/Windows/private local resources; ordinary repository work should be handled by remote/hybrid agents where possible.

Read `CI_POLICY.md` and `AGENTS.md` first, then `docs/CI-READINESS.md`, before changing CI or running any GitHub Action.
