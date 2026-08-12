# Work claim — Project Browser workspace state smoke contract

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T08:24:00+07:00`
- Completed: `2026-08-12T08:25:00+07:00`
- Baseline main SHA: `cc3d339a78546ed9fa06d466f43ce24274b95115`
- Priority: P1 — keep Core smoke expectations aligned with the current persisted dirty-tracking contract.

## Confirmed defect

The legacy `ProjectBrowserWorkspaceStateStoreSmoke` still asserted the superseded contract that real persisted workspace Save/Clear mutations must not advance `ProjectState.ChangeVersion`. Production behavior and the separately registered dirty-tracking smoke intentionally require those mutations to advance the version exactly once, so the Core smoke suite carried contradictory expectations.

## Completed work

- `SaveLoadRoundTripsValidatedState()` now requires a changed Save to advance `ChangeVersion` exactly once.
- The old `PresentationStateDoesNotInvalidateSemanticVersion()` check was renamed/aligned to `PresentationStateTracksPersistenceVersion()` and requires both Save and Clear to advance exactly once.
- `ClearRemovesPersistedState()` now requires the first Clear to advance exactly once while preserving the second absent-key Clear as an idempotent version no-op.
- Repeated identical Save remains a version no-op.
- Existing serialization/load/corruption/schema/stale-selection assertions remain unchanged.

## Exact implementation surfaces

- `tests/QS3D.Core.SmokeTests/ProjectBrowserWorkspaceStateStoreSmoke.cs`
- this claim file

## Integration evidence

- Claim registration: `ab8a01d437d23c44585f975bd17c789eb9f4b0ff`.
- Smoke contract fix on `main`: `63b06496c6996bd769a44ef88b88afb7b13c2203`.
- Post-write readback from `main` shows the three changed version assertions use exact `+ 1` semantics and idempotent repeated Clear snapshots the already-cleared version.
- `ProjectBrowserWorkspaceDirtyTrackingRegistration.cs` was confirmed present before this lane; no registry/module-initializer change was needed.

## Exclusions and validation boundary

No production `ProjectBrowserWorkspaceStateStore`, Browser query/selection/grouping/XML-schema/native/WPF, or shared smoke registry changes were made. GitHub Actions were not dispatched; executable smoke/build PASS and BricsCAD runtime PASS are not claimed.
