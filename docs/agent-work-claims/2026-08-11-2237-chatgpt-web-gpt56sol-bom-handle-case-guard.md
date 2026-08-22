# Work claim — BOM live generated handle case guard

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T22:37:00+07:00`
- Baseline main SHA: `885dd9ae43a34fb7918caf0ea9981ba0aed8f61b`
- Priority: continue-all remote-safe release diagnostics correctness

## Reserved scope

Make BOM release generated-handle liveness matching honor the repository's established case-insensitive CAD Handle identity contract regardless of the comparer used by the caller-provided `ISet<string>`.

## Expected surfaces

- `src/QS3D.Core/Diagnostics/BomReleaseGuardService.cs`
- `tests/QS3D.Core.SmokeTests/BomReleaseGuardSmoke.cs`
- this claim file

## Excluded scope

- No ownership-slot parsing changes.
- No quantity/report grouping behavior changes.
- No BricsCAD V25/native/UI changes.
- No updater, persistence, authoring, curtain, tag, release packaging or workflow changes.
- No GitHub Actions dispatch.

## Completed changes

- `8633a08af3b49332231cb24a616082e17a40a98a` — normalize the caller-provided live generated Handle set into an `OrdinalIgnoreCase` index once at the BOM boundary and share that index with curtain-panel health plus BOM ownership liveness checks.
- `bf9cb1b8acb81b76832b54be0ca788f9b1de786b` — add regression coverage using uppercase semantic ownership with lowercase live Handle supplied through an ordinal case-sensitive `HashSet<string>`.

## Validation performed

- Re-fetched current source and focused smoke before each write.
- Confirmed the repository's generated-handle ownership code uses case-insensitive Handle identity, while the previous BOM liveness check inherited the caller set comparer.
- Regression keeps true missing-handle coverage intact and adds comparer-independent case matching.
- No GitHub Actions were dispatched and no BricsCAD V25 runtime verification is claimed from this environment.

## Coordination

This lane remained limited to BOM live-handle lookup semantics and its existing focused smoke. No neighboring quantity/report or native ownership lane was changed.
