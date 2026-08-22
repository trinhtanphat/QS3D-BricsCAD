# Work claim — Interchange append target dependency integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-interchange-append-target-dependency-integrity-20260812-0917`
- Registered: `2026-08-12T09:17:00+07:00`
- Completed: `2026-08-12T09:20:00+07:00`
- Baseline main SHA: `e8558edf801e462085e4027967ff32397982be1b`
- Claim commit: `5c64031980be4330749781df2a131a459c755885`
- Source fix commit: `51103db8d8dc4a3b0ac36e051de71a38da7f85a0`
- Regression commit: `30519442b6962b352867dfcf72733828b703bd55`

## Completed scope

`ProjectInterchangeAppendOnlyImporter.ValidateTarget` now builds a case-insensitive dependency identity set per target element and fails closed when the same dependency appears more than once. This prevents append preflight from accepting an already-malformed target with exact or case-only duplicate dependency identities while preserving the existing missing-dependency validation.

## Implemented surfaces

- `src/QS3D.Core/Export/ProjectInterchangeAppendOnlyImporter.cs`
- `tests/QS3D.Core.SmokeTests/ProjectInterchangeAppendOnlyImporterSmoke.cs`
- this claim file

## Regression coverage

The focused smoke now covers both exact duplicate `TGT-E1` / `TGT-E1` and case-only duplicate `TGT-E1` / `tgt-e1`. Both `Plan` and `Import` must reject before mutation, preserving target element count, dependency list, audit count, metadata count and `UpdatedUtc`. The pre-existing successful append smoke continues to cover a valid single dependency.

## Validation actually performed

- Re-read the integrated importer from current `main` and confirmed `HashSet<string>(StringComparer.OrdinalIgnoreCase)` duplicate detection runs before dependency existence validation.
- Re-read the integrated append-only smoke and confirmed exact/case-only duplicate coverage plus no-mutation assertions.
- Verified regression commit `30519442b6962b352867dfcf72733828b703bd55` is an ancestor of main snapshot `05caa11bdc067f1ba431a524b67b32ba6211ca08` with `behind_by: 0`; the two intervening commits touched only `ProjectFloorService.cs` and `CurtainWallPanelFingerprint.cs`.
- No GitHub Actions were dispatched. No local .NET build/smoke execution or BricsCAD V25/V26 runtime PASS is claimed.

## Excluded scope honored

No dependency cycle/self-dependency redesign, source JSON schema changes, UI/BricsCAD adapter changes, provenance changes or CAD ownership changes were made.

## Completion

Completed. Append-only interchange now rejects duplicate target dependency identities before mutation and records exact integration SHAs.