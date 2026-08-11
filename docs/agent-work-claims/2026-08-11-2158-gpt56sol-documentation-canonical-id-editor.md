# Work claim — Semantic documentation canonical ID editor regression

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-documentation-id-20260811-2158`
- Registered: `2026-08-11T21:58:18+07:00`
- Baseline main SHA: `fe14f9b084fcf7609b636d0ce97c5fd3dcd64f08`
- Priority: P2 source-proven bug/regression hardening

## Reserved scope

Fix the semantic documentation catalog editor identity mismatch where definitions accepted by the documentation planners may retain surrounding whitespace in persisted/raw IDs, while editor matching compares the raw stored ID to a normalized requested ID. This can make an accepted view impossible to remove/replace through its canonical ID.

## Expected surfaces

- `src/QS3D.Core/Documentation/SemanticDocumentationCatalogEditor.cs`
- `tests/QS3D.Core.SmokeTests/SemanticDocumentationCatalogEditorSmoke.cs`
- this claim file for close-out status

## Explicit exclusions

- No changes to BricsCAD command/UI/runtime surfaces.
- No changes to documentation XML schema/version or broad persistence architecture.
- No changes to `SemanticDocumentationCatalogStore.cs` unless new source evidence proves the narrow editor fix cannot preserve the current storage contract; any scope expansion must be registered first.
- No changes to generic Core mutation/persistence lanes reserved by other active claims.
- No GitHub Actions dispatch or workflow edits.

## Validation plan

- Add a focused regression proving an accepted whitespace-padded view ID can be addressed by its canonical trimmed ID through editor operations.
- Keep ID matching case-insensitive and normalize both stored/raw selectors and requested IDs at the comparison boundary.
- Preserve existing sheet-reference rewrite semantics and fail-closed duplicate detection.
- Re-read current `main`, this source file, smoke test, and active claims before the implementation push.
- Validation is source/static plus focused smoke-test source review in this remote environment; do not claim BricsCAD V25 runtime or GitHub Actions execution.

## Coordination

This lane is limited to `QS3D.Core/Documentation` editor identity matching and its existing smoke file. It does not overlap the active Core mutation atomicity claim, whose current reserved implementation surfaces are Navigation/Review/Interchange/Rules plus focused QSDB/ProjectSession persistence work.

## Completion condition

The canonical-ID editor defect is fixed on current `main`, focused regression coverage is added, the final diff is re-read against latest `main`, and this claim is marked `COMPLETED` with validation actually performed.
