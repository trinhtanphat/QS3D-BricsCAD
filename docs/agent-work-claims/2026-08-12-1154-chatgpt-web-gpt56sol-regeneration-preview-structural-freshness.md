# Work claim — Regeneration Preview structural freshness

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-regeneration-preview-structural-freshness-20260812-1154`
- Registered: `2026-08-12T11:54:00+07:00`
- Completed: `2026-08-12T12:01:00+07:00`
- Baseline main SHA: `8987124254dd3f6ad79563ab1cbcfc43ea865475`
- Claim commit: `efff914eeba9604511cff876895496f180a2e7fb`
- Source fix commit: `9065591378905e904ffde957d539365243f67167`
- Focused smoke commit: `5d6c37307640d72953cb33f58b53cb13ac04897e`
- Integration PR: `#852`
- Main integration SHA: `d6d3959d8ca04ca16aeed706ca594d2edb3398cb`
- Priority: P1 — subset preview must not silently detach from a different semantic element structure under the same ChangeVersion.
- Task Key: `CORE-REGENERATION-PREVIEW-STRUCTURAL-FRESHNESS`

## Confirmed defect

The completed subset input-freshness lane captured `ProjectState.ChangeVersion` and element count before caller-controlled `elementIds` enumeration. That caught ordinary mutations that call `Touch()`, but `ProjectState.Elements` remains publicly mutable. A lazy target enumerable could directly remove or replace semantic element entries without advancing `ChangeVersion`; `PreviewSubset(...)` then detached from the changed structure and could stamp that preview with the pre-enumeration revision.

## Implemented contract

- `PreviewSubset(...)` snapshots project element ID -> instance ownership before enumerating caller targets.
- The existing target cardinality bound uses the ownership snapshot count.
- After target canonicalization, count/null/duplicate/removal/same-ID replacement drift is rejected even when `ChangeVersion` is unchanged, before detached preview construction.
- Ownership is re-checked after detached preview construction before the preview is returned.
- Existing `ChangeVersion` freshness errors remain first for ordinary semantic mutation.
- Full-project preview behavior, canonical target validation, detached regeneration, health/revision comparison, apply guards, `RegenerationEngine`, `DependencyGraph`, persistence and native/UI code were not changed.

## Regression evidence

`tests/QS3D.Core.SmokeTests/RegenerationPreviewStructuralFreshnessSmoke.cs` uses the established Beam regeneration fixture. A lazy target enumerable replaces non-target `B2` with a new same-ID instance through the public `project.Elements` list without calling `Touch()`. The regression requires structural freshness rejection while `ChangeVersion` remains unchanged and live target quantities remain unmodified. A stable `B1` subset control still produces a scoped, read-only preview.

## Integration / concurrency evidence

The branch diff from claim commit contained exactly the reserved source plus the new focused smoke. Nine commits between claim `efff914e...` and reviewed moving `main@f313bd87...` did not touch `RegenerationPreviewService.cs` or the new smoke; the nearby `RegenerationWorkProfiler` structural-freshness lane was a separate file. Current-main readback immediately before merge still had the exact pre-fix source blob `5f2b158bcf17a62e244b12c0ef49df7a1d5e5310`. PR #852 was squash-merged with expected head `5d6c37307640d72953cb33f58b53cb13ac04897e` as `d6d3959d8ca04ca16aeed706ca594d2edb3398cb`.

## Validation boundary

No GitHub Actions were dispatched. No force-push was used. No local .NET/full executable smoke or licensed BricsCAD V25/V26 runtime PASS is claimed from this connector-only lane.
