# Work claim — Regeneration Preview structural freshness

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-regeneration-preview-structural-freshness-20260812-1154`
- Registered: `2026-08-12T11:54:00+07:00`
- Baseline main SHA: `8987124254dd3f6ad79563ab1cbcfc43ea865475`
- Priority: P1 — subset preview must not silently detach from a different semantic element structure under the same ChangeVersion.
- Task Key: `CORE-REGENERATION-PREVIEW-STRUCTURAL-FRESHNESS`

## Confirmed defect

The completed subset input-freshness lane captures `ProjectState.ChangeVersion` and element count before caller-controlled `elementIds` enumeration. That catches ordinary mutations that call `Touch()`, but `ProjectState.Elements` remains publicly mutable. A lazy target enumerable can directly remove or replace semantic element entries without advancing `ChangeVersion`. `PreviewSubset(...)` then calls `PreviewInternal(...)`, which sees the unchanged revision and creates a detached copy from the structurally changed project, allowing a preview to be stamped with the pre-enumeration revision while using different element ownership.

## Reserved scope

- `src/QS3D.Core/Services/RegenerationPreviewService.cs`
- `tests/QS3D.Core.SmokeTests/RegenerationPreviewStructuralFreshnessSmoke.cs`
- this claim file

## Intended contract

- Snapshot project element ID -> instance ownership before enumerating caller subset targets.
- After target canonicalization, reject count/null/duplicate/removal/same-ID replacement drift even if `ChangeVersion` is unchanged, before detached-copy preview construction.
- Re-check ownership after preview construction before returning the preview so concurrent/reentrant structural drift cannot receive a fresh preview token.
- Preserve existing ChangeVersion freshness error/semantics, target cardinality/canonicality/duplicate validation, detached regeneration, health/revision comparison, full-project preview behavior and apply guards.
- Do not change `RegenerationEngine`, `DependencyGraph`, public collections, native/UI code or persistence.

## Validation plan

Add focused auto-registered Core smoke coverage where a lazy subset target source removes/replaces a non-target project element without `Touch()`. The preview must fail structural freshness while `ChangeVersion` remains unchanged. Include a stable subset control.

## Validation boundary

No GitHub Actions will be dispatched. No local .NET/full executable smoke or licensed BricsCAD V25/V26 runtime PASS will be claimed unless actually executed.
