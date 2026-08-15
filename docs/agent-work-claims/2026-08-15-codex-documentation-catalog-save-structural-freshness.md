# Work claim — Documentation catalog save structural freshness

- Status: `ACTIVE`
- Agent: `Codex /root/audit_documentation_gap`
- Registered: `2026-08-15T08:27:43+07:00`
- Baseline main SHA: `b0fe2bb88206ddab1cb99ae1c1154838f6eaa6b3`
- Issue: `#77`
- Priority: remote-safe Core documentation correctness; approved bounded successor scope

## Confirmed defect

`SemanticDocumentationCatalogStore.Save(...)` materializes caller-controlled `views` and then `sheets` before `SemanticViewPlanner.BuildCatalog(...)` captures project freshness. A lazy input can replace an entry in the public mutable `ProjectState.Elements` list with a different same-ID instance without advancing `ChangeVersion`, then yield an otherwise valid definition. The store currently validates and persists the catalog against the structurally changed project instead of rejecting state drift across caller enumeration.

## Reserved scope

- `src/QS3D.Core/Documentation/SemanticDocumentationCatalogStore.cs`: capture `ChangeVersion` plus ordered reference identity for project Elements/Floors/Zones before external view/sheet enumeration; reject drift after each enumeration and immediately before persistence mutation.
- `tests/QS3D.Core.SmokeTests/SemanticDocumentationCatalogSaveStructuralFreshnessSmoke.cs` plus one registration file: deterministic same-ID element replacement, revision-only drift and stable controls across both view and sheet enumeration.
- `scripts/preflight-semantic-documentation-catalog-save-structural-freshness.py`: focused ordering/coverage guard.
- This claim file for closeout only.

## Excluded scope

- No `SemanticDocumentationTableBuilder` work reserved by the active structural-freshness claim.
- No editor behavior, XML schema/format, planner semantics, bounds, ordering, renderer, schedule catalog, tag or native BricsCAD documentation changes.
- No LOCAL probes/runners, V25/native UI/runtime, private data, release/signing, workflows or GitHub Actions.
- Broad issue `#77` remains open.

## Validation plan

- Run the focused smoke/static gate and relevant documentation preflights.
- Build `QS3D.Core` and `QS3D.Core.SmokeTests` in Release.
- Run the full deterministic Core smoke suite and aggregate preflight; report any independent failure without expanding this claim.
- Re-fetch before every write/merge, preserve concurrent work, and merge normally without force push.

## Coordination

The active documentation-table claim reserves only `SemanticDocumentationTableBuilder` and its focused smoke. Open PRs observed at registration do not touch this Store/save-freshness surface. Parent `/root` approved this exact bounded split.

## Completion condition

The claim is visible on `origin/main` before implementation, the Store fails closed on revision or ordered project reference-identity drift across both input enumerations, focused regression evidence is merged, and this claim records exact SHAs while leaving #77 open.
