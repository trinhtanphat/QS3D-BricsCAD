# Work claim — Interchange append target dependency integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-interchange-append-target-dependency-integrity-20260812-0917`
- Registered: `2026-08-12T09:17:00+07:00`
- Baseline main SHA: `e8558edf801e462085e4027967ff32397982be1b`

## Confirmed defect

`ProjectInterchangeAppendOnlyImporter.ValidateTarget` verifies that every target dependency resolves to an existing semantic element, but it does not reject duplicate dependency identities on the same element. Because the target element index is case-insensitive, malformed lists such as `E-BASE` plus `e-base` pass append preflight even though `DependencyGraph` treats duplicate dependencies as invalid project state. Append-only import can therefore accept an already-malformed target and proceed with mutation.

## Reserved scope

- `src/QS3D.Core/Export/ProjectInterchangeAppendOnlyImporter.cs`
- focused regression in the existing `ProjectInterchangeAppendOnlyImporterSmoke`
- this claim file

Reject duplicate target dependency identities case-insensitively before append mutation while preserving valid single dependencies, existing missing-dependency checks, append collision semantics, rollback/provenance behavior and source-snapshot validation. Do not redesign dependency cycles/self-dependency policy, source JSON schema, UI, BricsCAD adapters or ownership semantics.

## Completion

Complete only after source + focused regression are on current `main`, exact SHAs are recorded here, and this claim is marked `COMPLETED`. No GitHub Actions, local .NET build or BricsCAD runtime qualification is claimed by this remote lane.