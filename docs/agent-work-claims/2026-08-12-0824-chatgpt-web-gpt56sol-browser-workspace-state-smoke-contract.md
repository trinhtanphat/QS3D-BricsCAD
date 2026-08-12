# Work claim — Project Browser workspace state smoke contract

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T08:24:00+07:00`
- Baseline main SHA: `cc3d339a78546ed9fa06d466f43ce24274b95115`
- Priority: P1 — keep Core smoke expectations aligned with the current persisted dirty-tracking contract.

## Reserved scope

Align the legacy `ProjectBrowserWorkspaceStateStoreSmoke` with the newer persisted workspace dirty-tracking contract. Production `ProjectBrowserWorkspaceStateStore.Save/Clear` intentionally advances `ProjectState.ChangeVersion` exactly once for a real metadata mutation, while the old smoke still asserts that those mutations must not change the semantic version. A separate registered dirty-tracking smoke already pins the new behavior, so the current suite is internally contradictory.

## Exact implementation surfaces

- `tests/QS3D.Core.SmokeTests/ProjectBrowserWorkspaceStateStoreSmoke.cs`
- this claim file

## Exclusions

- No production `ProjectBrowserWorkspaceStateStore` changes.
- No shared smoke registry or module-initializer changes; `ProjectBrowserWorkspaceDirtyTrackingRegistration.cs` is present and already registers the focused dirty-tracking smoke.
- No Browser query/selection/grouping/XML-schema/native/WPF changes.
- No GitHub Actions dispatch and no BricsCAD runtime claim.

## Evidence

- Historical commit `c2eaf02d45cadeed85e3ffe6da148ec8d3043473` introduced the older no-version-change expectation.
- The later dirty-tracking lane (`ceeeb0f96eef26dd0563e59f11ca0ddd084eb47d`, closed by `7fd308033bdb3cff012bfe44742cf4c81a8206ef`) intentionally changed persisted Save/Clear mutations to advance `ChangeVersion` and added focused coverage.
- Current `ProjectBrowserWorkspaceStateStoreSmoke` still contains the superseded equality assertions, while the focused dirty-tracking smoke expects exact +1 mutation semantics.

## Completion condition

- Save of a changed workspace must be asserted to advance `ChangeVersion` exactly once.
- Clear of existing workspace metadata must be asserted to advance `ChangeVersion` exactly once.
- Existing serialization/load/clear behavior assertions remain intact.
- Re-read the changed file and close this claim with the exact commit SHA; do not claim unexecuted CI/runtime PASS.
