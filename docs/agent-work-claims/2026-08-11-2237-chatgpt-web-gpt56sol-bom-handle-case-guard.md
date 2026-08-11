# Work claim — BOM live generated handle case guard

- Status: `ACTIVE`
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

## Validation plan

- Add a deterministic smoke where project/generated ownership uses uppercase CAD Handle while caller supplies the same live handle in lowercase through an ordinal case-sensitive `HashSet<string>`; BOM release must not report `BOM_GENERATED_HANDLE_MISSING`.
- Preserve true missing-handle blocking and existing canonical owner-slot behavior.

## Coordination

This lane is limited to BOM live-handle lookup semantics and its existing focused smoke. It does not take neighboring quantity/report or native ownership lanes.

## Completion condition

Source guard + regression are pushed to current `main`, then this claim is marked `COMPLETED` with exact SHAs and validation actually performed.
