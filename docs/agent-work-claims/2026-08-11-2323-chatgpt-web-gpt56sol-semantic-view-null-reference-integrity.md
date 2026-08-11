# Work claim — Semantic view null-reference integrity

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T23:23:00+07:00`
- Completed: `2026-08-11T23:27:00+07:00`
- Baseline main SHA: `58eb62f880f18564d06a8d77a9d6038438de4ea1`
- Priority: evidence-driven remote-safe Core documentation hardening

## Reason

`SemanticViewPlanner.Build()` validated a requested Floor/Zone by projecting `project.Floors.Select(x => x.Id)` / `project.Zones.Select(x => x.Id)`. A corrupted or partially deserialized project could contain a null Floor/Zone entry because the public `IList<T>` collections accept null at runtime. The planner then leaked `NullReferenceException` instead of following the existing `ProjectState` fail-closed contract, where null semantic collection entries produce a controlled `InvalidOperationException`.

## Reserved scope

Fail closed deterministically when a semantic view resolves a Floor/Zone reference against a collection containing a null entry, while preserving current missing-reference, duplicate-reference, case-insensitive identity, filtering, ordering, and bounded-input behavior.

## Completed surfaces

- `src/QS3D.Core/Documentation/SemanticViewPlanner.cs`
- `tests/QS3D.Core.SmokeTests/SemanticViewNullReferenceSmoke.cs`
- `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs`
- this claim file

## Result

- `1cdcf77393a67c503f13b0506a14512dc6424665` — `SemanticViewPlanner` now enumerates Floor/Zone definitions through a null-safe reference resolver and preserves existing missing/ambiguous/case-insensitive behavior.
- `901950917dc2724741af86268cc67082e3920cb4` — added focused Floor/Zone null-reference regression smoke that requires `InvalidOperationException` and rejects `NullReferenceException` leakage.
- `f9ca3b851d2556e40fad107bc4929e4f3cf33958` — registered the focused smoke in the deterministic Core smoke suite.
- Current `main` blobs were re-read after those writes and still contain the intended resolver, smoke, and registration.

## Validation boundary

- Source/static verification completed against current `main`.
- The hosted shell cannot resolve GitHub for a local checkout and does not have `gh`, so no repository `dotnet` execution is claimed from this session.
- No GitHub Actions were dispatched.
- No BricsCAD V25 runtime PASS is claimed.

## Excluded scope

- No native BricsCAD view/layout materialization changes.
- No Sheet Index, title-block, Interchange, persistence schema, or project collection API changes.
- No unrelated smoke registrations changed.

## Coordination

No overlapping semantic-view claim appeared before registration. The lane is now released/completed; future work must re-read current `main` and current claims before touching these surfaces.