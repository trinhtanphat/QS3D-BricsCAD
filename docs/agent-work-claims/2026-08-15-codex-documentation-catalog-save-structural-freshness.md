# Work claim — Documentation catalog save structural freshness

- Status: `COMPLETED`
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

## Completion evidence

- Claim commit `718227ebaffc4f6e3a40bbe59450dce00633b6dc` was merged first through PR `#1440` at `bbf44df6f5440566758122866289ea60973e155c` and verified as an ancestor of `origin/main` before implementation.
- Implementation commit `e43d655f97c11fa7480a9dabf0ece30a90d0c4dc` was merged normally through PR `#1448` at `f8f5c867c97999f5590dc207cc45925574a0cfa1`.
- `SemanticDocumentationCatalogStore.Save(...)` now snapshots `ChangeVersion` plus ordered `Elements` / `Floors` / `Zones` reference identities before external enumeration, checks after views, after sheets, after planners and immediately before either persistence mutation.
- The one module-registered deterministic smoke covers same-ID element replacement and revision drift in both view and sheet enumerators plus stable first-save/repeated-save behavior.
- `QS3D.Core` Release build: `0` warnings, `0` errors.
- `QS3D.Core.SmokeTests` Release build: `0` warnings, `0` errors; full deterministic run: `ALL PASS`.
- Focused new static gate: `PASS`; all `41` documentation/view/sheet/schedule/tag/title-block preflights: `PASS`; aggregate preflight: `808/808 PASS`.
- No native/V25/UI/runtime, LOCAL automation, private data, release/signing or GitHub Actions operation was performed. Broad issue `#77` remains open.
