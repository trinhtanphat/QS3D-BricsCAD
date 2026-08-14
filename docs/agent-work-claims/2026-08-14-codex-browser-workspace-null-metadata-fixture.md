# Work claim — Browser workspace null-metadata fixture reconciliation

- Status: `COMPLETED`
- Agent: `codex-browser-workspace-null-metadata-fixture-20260814` (`/root/fix_level_curtain_frame_z`, delegated by `/root`)
- Registered: `2026-08-14T13:57:06+07:00`
- Completed: `2026-08-14T14:00:06+07:00`
- Baseline main SHA: `2ac289098c73e9873d466349701f1d6264c589d7`
- Priority: continue the first observable contained Core smoke blocker after Preview Review reconciliation

## Diagnosis

`ProjectBrowserWorkspaceEmptyMetadataSmoke.NullMetadataFailsWithoutMutation` requests a null metadata value, then compares that caller input directly with the value stored after `ProjectBrowserWorkspaceStateStore.Load` rejects it. Current `ProjectMetadataDictionary` correctly canonicalizes null to `string.Empty` immediately at the supported metadata write boundary, so the later null-versus-empty comparison falsely attributes pre-load canonicalization to `Load`.

The completed generic metadata persistability contract requires immediate null-to-empty canonicalization. The completed Browser null-metadata contract still requires the present null request, canonicalized as present empty state, to fail closed with `InvalidDataException` without Load changing the metadata value, `UpdatedUtc`, or `ChangeVersion`.

## Reserved scope

- `tests/QS3D.Core.SmokeTests/ProjectBrowserWorkspaceEmptyMetadataSmoke.cs`
- this claim file
- parent LOCAL-003 claim only for the explicit delegation/completion record

After the existing null/empty/whitespace request is assigned, capture the canonical stored value before `Load`. Explicitly pin that a null request stores empty text, then compare the post-failure value against the captured pre-load value. Retain the present-key, `InvalidDataException`, metadata presence, `UpdatedUtc`, and `ChangeVersion` assertions for every case.

## Excluded scope

No production metadata/browser/workspace/domain/persistence change, no runner/module-initializer architecture or adjacent fixture, and no Level production, probe, runner, BricsCAD, private data, GitHub Actions, V26, release or packaging change.

## Validation and completion

Run the strict Core smoke Release build, registered full Core smoke, and focused Browser/workspace/metadata gates. If the complete smoke reaches a separate stale fixture, report it without expanding this claim. Merge the test-only correction through a normal PR, record exact SHAs, then mark this claim `COMPLETED`.

## Completion record

- Claim-only PR `#1178` merged as `b238052408fc2531e4a14ef142a3693e6edd3b05` before the test edit.
- Implementation source commit `8bcfb30f1ee44332a24bcaba613ed17336ed8300` merged through PR `#1179` as `e23c7473a3783407a4ebc4d2db38a4848e1f644e`.
- The fixture retains the null request and present-key fail-closed path, explicitly pins immediate null-to-empty metadata canonicalization, and compares post-rejection metadata against the canonical value captured before Load while preserving `UpdatedUtc` and `ChangeVersion` assertions.
- Core smoke Release build passed with zero warnings/errors. All six focused Browser/workspace/metadata gates passed. The complete registered smoke advanced through this fixture and then stopped at the independent `ProjectQuantityReportGroupKeySmoke.DelimiterInjectionDoesNotMergeDistinctRows` U+001F Family-ID fixture; Project Quantity remains unchanged and outside this claim.
- No production, domain, runner/module-initializer, Level, probe/runner, BricsCAD, private-data or GitHub Actions surface was changed or executed.
