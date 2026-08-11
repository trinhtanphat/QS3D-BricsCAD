# Work claim — Semantic view null-reference integrity

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T23:23:00+07:00`
- Baseline main SHA: `58eb62f880f18564d06a8d77a9d6038438de4ea1`
- Priority: evidence-driven remote-safe Core documentation hardening

## Reason

`SemanticViewPlanner.Build()` validates a requested Floor/Zone by projecting `project.Floors.Select(x => x.Id)` / `project.Zones.Select(x => x.Id)`. A corrupted or partially deserialized project can contain a null Floor/Zone entry because the public `IList<T>` collections accept null at runtime. The planner then leaks `NullReferenceException` instead of following the existing `ProjectState` fail-closed contract, where null semantic collection entries produce a controlled `InvalidOperationException`.

## Reserved scope

Fail closed deterministically when a semantic view resolves a Floor/Zone reference against a collection containing a null entry, while preserving current missing-reference, duplicate-reference, case-insensitive identity, filtering, ordering, and bounded-input behavior.

## Expected surfaces

- `src/QS3D.Core/Documentation/SemanticViewPlanner.cs`
- `tests/QS3D.Core.SmokeTests/SemanticViewNullReferenceSmoke.cs`
- `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs`
- this claim file

## Excluded scope

- No native BricsCAD view/layout materialization changes.
- No Sheet Index, title-block, Interchange, persistence schema, or project collection API changes.
- No changes to unrelated smoke registrations beyond adding this focused smoke entry.
- No GitHub Actions dispatch and no BricsCAD V25 runtime PASS claim.

## Validation plan

- Add a focused dedicated smoke for null Floor and null Zone entries with matching semantic-view filters; both must fail with the existing domain-level `InvalidOperationException` path rather than `NullReferenceException`.
- Register only that smoke in the existing deterministic smoke runner.
- Preserve existing missing and ambiguous Floor/Zone semantics.
- Re-fetch current `main`, claims, and every target blob immediately before writes; never force-push.
- Record source/static verification only if this hosted session cannot execute the repository build.

## Coordination

No active claim filename matching semantic-view scope was present in the current claim registry at registration time. This lane remains limited to the pure-Core planner plus focused smoke coverage; the dedicated smoke avoids replacing the larger shared semantic-view/sheet smoke file.

## Completion condition

Current `main` fail-closes null Floor/Zone project references during semantic view planning, includes registered focused regression coverage, and this claim is marked `COMPLETED`.