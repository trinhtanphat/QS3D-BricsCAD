# Work Claim: BricsCAD V25 preview.3 release version

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Started: `2026-08-12T00:53:00Z`
- Last update: `2026-08-12T00:57:00Z`
- Baseline commit: `c32edc9f576935da7ef44fba74980dbb6b3d68e5`

## Scope
- Prepare the source package version for the next owner-authorized BricsCAD V25 cloud preview release.
- Advance the Core and V25 package metadata from `0.1.0-preview.2` to `0.1.0-preview.3` only while that remains the current version and `v0.1.0-preview.3` remains unused.

## Files / areas
- `src/QS3D.Core/QS3D.Core.csproj`
- `src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj`
- This claim file.

## Exclusions
- No workflow trigger changes.
- No unrelated source, UI, runtime, installer, or packaging changes.
- No overwrite or reset of concurrent agent work.

## Implementation
- Claim registration: `fac2b3f6bdd42aec258232011d5aa9d23e4df1fc`.
- Core package metadata: `a8ea8e92a23b86146f7fe69d67317434339b495d`.
- V25 package metadata: `8b75fa34ba5a58817e2a17adafa2a9a0e38fab8d`.
- A preliminary atomic commit object `7a06c8b2d8939e8b369149dd817df31ab4c4bde8` was intentionally not attached to `main` after a non-fast-forward guard detected concurrent work; no force update was used.

## Validation
- Read back both project files from `main`: `Version=0.1.0-preview.3`, `FileVersion=0.1.0.3`, `InformationalVersion=0.1.0-preview.3`, and `AssemblyVersion=0.1.0.0`.
- Confirmed `v0.1.0-preview.3` did not exist immediately after the source version update.
- No GitHub Actions success is claimed here; the owner-authorized cloud V25 release requires a fresh workflow dispatch on the current `main` SHA.
