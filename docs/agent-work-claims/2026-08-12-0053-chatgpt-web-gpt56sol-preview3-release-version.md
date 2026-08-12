# Work Claim: BricsCAD V25 preview.3 release version

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Started: `2026-08-12T00:53:00Z`
- Last update: `2026-08-12T00:53:00Z`
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

## Validation
- Confirm both projects expose matching `Version`, `FileVersion`, and `InformationalVersion` metadata for preview.3 while preserving `AssemblyVersion`.
- Confirm `v0.1.0-preview.3` does not already exist before release dispatch.
- Use a fresh cloud V25 workflow dispatch after implementation; do not rerun a stale-SHA release run.
