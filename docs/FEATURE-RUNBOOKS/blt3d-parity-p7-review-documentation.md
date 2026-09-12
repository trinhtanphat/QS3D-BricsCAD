# BLT3D parity P7 — view, quantity, revision, drawing documentation

## Scope

P7 continues the clean-room BLT3D parity program after P1–P6. It records recovered workflow/UX inventory only and binds current QS3D workflows when explicit user-facing command evidence already exists.

No recovered BLT3D implementation, binary, private API, asset, key, license behavior, or runtime dependency is copied into QS3D. The recovered material is used only to name workflow/UX expectations.

The repository manifest remains `# catalog-complete=false`. P7 does not claim full BLT3D parity and does not advance any workflow to `SemanticBehaviorPass`, `SaveReopenPass`, or `V25V26ParityPass`.

## Command-wired workflows

The following rows are `Applicable` / `CommandWired` because current QS3D exposes explicit commands and corresponding UI/service paths:

- `view` — `QS3DVIEW3D`, `QS3DVIEWTOP` and the existing viewport command surface.
- `quantity` — `QS3DBQ` plus the existing Quantity Summary / Quantity Insight / native-table workflow.
- `revision` — `QS3DREVBASE`, `QS3DREVDIFF`, `RevisionWindow`, and the existing Core revision services.

P7 represents these three workflows as host-neutral, read-only UI bindings that require an active document. The catalog is routing/evidence metadata only; it does not replace BricsCAD command implementations or Core business logic.

## Drawing Manager evidence ceiling

`drawing-manager` remains `Applicable` / `ReferenceCaptured`.

Current QS3D source does contain related documentation infrastructure: the drawing-bound `Project Browser` surface and semantic sheet commands such as `QS3DSHEETBUILD`, `QS3DSHEETREFRESH`, `QS3DSHEETREMOVE`, and `QS3DSHEETHEALTH`.

Those neighboring capabilities do not yet prove one canonical QS3D workflow equivalent to the recovered BLT3D Drawing Manager. P7 therefore deliberately leaves Drawing Manager unbound in `ParityReviewDocumentationCatalog` and does not infer parity from partial infrastructure.

A later carrier may advance this row only after it identifies a canonical user workflow and qualifies its behavior independently.

## Verification

Run from the repository root:

```powershell
python scripts/preflight-blt3d-parity-p7-review-documentation.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
git diff --check
```

The focused P7 preflight verifies:

- canonical manifest shape and fail-closed catalog metadata;
- `view`, `quantity`, and `revision` at `CommandWired`;
- `drawing-manager` held at `ReferenceCaptured`;
- exactly three host-neutral read-only UI bindings;
- deterministic Core smoke registration;
- current command evidence for the three wired workflows;
- Project Browser and semantic-sheet inventory without overclaiming Drawing Manager;
- this runbook's clean-room and evidence-ceiling statements.
