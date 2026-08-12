# Work claim — Interchange preview fingerprint parity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-interchange-preview-fingerprint-parity-20260812-0924`
- Registered: `2026-08-12T09:24:00+07:00`
- Baseline main SHA: `d86b7146b7de439f6de30cc26a9600812a571f15`

## Confirmed defect

`ProjectInterchangeImportPreview.CompareFingerprint` compares source/target drawing fingerprints as raw strings, while `ProjectInterchangeImportResolutionPlanner.CompareFingerprint` normalizes both with `Trim()` before comparison. `ProjectState.DrawingFingerprint` is a raw settable string, so a target such as `" FP "` is reachable. For the same canonical source fingerprint `"FP"`, Preview reports `Different` while ResolutionPlanner reports `Match`, producing contradictory read-only import planning results.

## Reserved scope

- `src/QS3D.Core/Export/ProjectInterchangeImportPreview.cs`
- focused parity regression in `tests/QS3D.Core.SmokeTests/ProjectInterchangeImportPreviewSmoke.cs`
- this claim file

Align Preview fingerprint normalization with the already-established ResolutionPlanner semantics. Preserve Unknown behavior for empty/whitespace-only values, ordinal fingerprint comparison, collision accounting and all mutation boundaries. Do not change source snapshot validation, resolution policy, append importer or BricsCAD UI/runtime.

## Completion

Complete only after source + focused regression are on current `main`, exact SHAs are recorded here, and this claim is marked `COMPLETED`. No GitHub Actions, local .NET build or BricsCAD runtime qualification is claimed by this remote lane.