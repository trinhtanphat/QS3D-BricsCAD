# BricsCAD V25 Beam Rebar UI source-document affinity

## Scope

Lane C03. `BeamRebarCommands` owns the source `Document` captured at command dispatch. Process-wide Workspace refresh/status must not be published for that command after a different MDI document becomes active.

## Deterministic / REMOTE_SAFE verification

Run:

```text
python scripts/preflight-v25-beam-rebar-ui-source-affinity.py
```

The guard requires `FinalizeUi` and `Report` to route process-wide Workspace publication through helpers that fail closed unless the captured source document is still the exact `Application.DocumentManager.MdiActiveDocument`. It also rejects CAD/project mutation, transaction ownership, command dispatch, and deferred dispatcher work inside those affinity helpers.

Run the repository aggregate feature guards, deterministic smoke, and BricsCAD V25 build against admitted locked reference generations. These are REMOTE_SAFE evidence only.

## Licensed BricsCAD V25 / LOCAL_ONLY

In a licensed V25 host, start `QS3DBEAMREBAR3D` from DWG A and force/simulate an MDI transition to DWG B before error reporting or post-commit UI finalization. Verify:

- A remains the source for editor output/regeneration attempts.
- B does not receive A's Workspace status.
- B is not refreshed by A's post-commit finalizer.
- A successful native/project mutation is not reported as rolled back merely because UI synchronization fails.

Do not claim `LOCAL_PASS` unless this scenario is exercised in a real licensed BricsCAD V25 runtime.