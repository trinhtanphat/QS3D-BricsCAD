# BLT3D parity P6 — rooms, finishes, earthwork, special categories

## Scope

P6 continues the clean-room BLT3D parity program after P1–P5. It records recovered workflow/UX evidence and binds only QS3D workflows that already have current command/service evidence.

The recovered reference inventory contains 61 XAML surfaces. P6 uses those surfaces only as behavioral inventory evidence; no recovered implementation, private API, binary, asset, key, license behavior, or runtime dependency is copied into QS3D.

The repository manifest must remain `# catalog-complete=false`. P6 does not claim full BLT3D parity and does not advance any row to `SemanticBehaviorPass`, `SaveReopenPass`, or `V25V26ParityPass`.

## Command-wired workflows

The following anchors are `Applicable` / `CommandWired` because current QS3D already exposes corresponding semantic command paths:

- `room` — `QS3DROOM` / `QS3DROOMAUTO`.
- `room.finish` — `QS3DFINISH` plus existing finish schedule/export lifecycle.
- `earthwork` — `QS3DEARTHWORK`.
- `stair` — `QS3DSTAIR`.
- `railing` — `QS3DRAILING`.
- `curtain` — existing Curtain Hub / `QS3DCURTAIN3D` family.
- `door-opening` — `QS3DDOOR` / `QS3DOPENING` and schedule lifecycle.
- `grid` — `QS3DGRID` and existing Grid lifecycle commands.

## Reference-only workflows

Recovered reference surfaces without sufficient current QS3D command/service evidence stay at `ReferenceCaptured`:

- `pile-cap` — `PileCapInputDialog`.
- `pile-cap.rebar` — `PileCapRebarDialog`.
- `pile.drop-to-cap` — `PileDropToCapWindow`.
- `steel-detail` — `SteelDetailWizard`.
- `copy-to-level` — recovered `CopyToLevelCommand` surface.
- `blinding-concrete` — `BlindingConcreteWindow` / blinding ribbon action.
- `opening-to-slab` — `OpeningToSlabWindow` / opening-to-slab ribbon action.

These rows must not be upgraded merely because a neighboring QS3D capability exists. Each later upgrade requires its own source evidence and qualification carrier.

## Host-neutral binding

`ParitySpecialCategoriesCatalog` contains only Core feature IDs, workflow kind/surface flags, and semantic requirements. It has no BricsCAD, Teigha, WPF, MCP, or launcher dependency.

All eight P6 bindings are semantic mutations on the UI surface and require active document, project, selection, atomic mutation, and audit boundaries. This catalog records routing requirements; it does not replace the existing QS3D semantic engine.

## Verification

Run from repository root:

```powershell
python scripts/preflight-blt3d-parity-p6-special-categories.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
git diff --check
```

The focused preflight verifies:

- canonical manifest shape and fail-closed metadata;
- eight P6 rows at `CommandWired`;
- seven recovery-only rows at `ReferenceCaptured`;
- host-neutral catalog tokens and forbidden host dependencies;
- deterministic Core smoke registration;
- current QS3D command evidence for each wired workflow;
- this runbook's clean-room and evidence-ceiling statements.

A green P6 static/Core result is not licensed BricsCAD runtime certification. Save/reopen and V25/V26 behavioral parity remain separate future evidence stages.
