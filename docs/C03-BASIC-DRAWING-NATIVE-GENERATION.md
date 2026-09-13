# C03 Basic Drawing native database generation fence

Issue: #6783  
Lane: C03 — BricsCAD V25 UI / Workspace / Modeless / Authoring  
Ownership-Key: `c03/v25-basic-drawing-native-generation-v1`

## Defect

`QS3DDRAWLINE`, `QS3DDRAWRECT`, and `QS3DDRAWCIRCLE` intentionally retain the command's Document, project/family/floor/zone context, and UCS across BricsCAD point/distance prompts. Those prompts are host-pumping boundaries. Before this carrier, freshness checks compared the managed `Document` wrapper plus project/UCS state but did not retain the native `Database.UnmanagedObject` identity.

BricsCAD can replace/reload the native database while the managed document wrapper survives. In that case a same-wrapper successor generation could satisfy the old managed-document check and receive geometry based on predecessor-generation command context. After a successful commit, implied selection, regen, and Workspace status could likewise target a successor generation and turn a real CAD success into misleading UI failure/status.

## Production contract

- `BasicDrawingContext` snapshots the exact native database identity at context capture.
- `RequireFreshContext` rejects native-generation replacement after interactive prompts before CAD mutation begins.
- `AppendEntity` revalidates the generation again after acquiring the document lock and before starting the write transaction. Existing lock/transaction/commit ownership remains unchanged.
- Once `Transaction.Commit()` succeeds, no rollback is attempted by UI finalization.
- `FinalizeSuccess` publishes implied selection, regen, warning, and success status only while the exact managed Document/native database generation is still current.
- Command-level exception reporting is also generation-bound so a stale command cannot publish a failure into a same-wrapper successor database.
- Existing project-generation, Family, Floor/Zone, Model Space, UCS, cancellation, XData identity, and exception-redaction contracts remain intact.

## Deterministic regression

`python scripts/preflight-v25-basic-drawing-native-generation.py`

The guard is auto-discovered by the aggregate feature-source preflight and checks native-generation capture plus ordering around the post-prompt check, post-lock transaction boundary, and post-commit UI publication.

## Runtime classification

`REMOTE_SAFE`:
- focused source/preflight regression;
- aggregate feature guards;
- deterministic smoke tests;
- trusted/admitted BricsCAD V25 reference validation;
- locked-reference V25 compile.

`LOCAL_ONLY` / licensed BricsCAD V25:
- same managed `Document` wrapper with native database reload/replacement during point/distance prompts;
- native-generation replacement after `LockDocument()` timing boundaries;
- visible implied-selection/regen/Workspace-status behavior after commit;
- real MDI/host-pumping timing.

Do not record `LOCAL_PASS` unless those scenarios are exercised in an actual licensed BricsCAD V25 runtime. Remote CI green is not a native runtime PASS.

## Self-review checklist

- document/native database lifetime and same-wrapper replacement;
- active-document and project generation switching;
- prompt cancellation and no accidental transaction start;
- document lock and transaction ownership;
- reentrancy/host-pumping boundaries;
- stale `ObjectId`/selection publication;
- post-commit UI sync and no rollback-after-commit;
- failure/status redaction and stale-generation suppression;
- thread/modeless ownership assumptions;
- V25 API compatibility and admitted-reference compile.
