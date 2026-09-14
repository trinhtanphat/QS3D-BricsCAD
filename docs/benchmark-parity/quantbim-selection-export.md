# QuantBIM standalone selection export

This P1 boundary publishes one Windows-standalone IFC selection as a deterministic interchange snapshot without introducing any BricsCAD dependency.

## Generation ownership

`QuantBimSelectionExportPublisher` accepts the existing `QuantBimStandaloneIfcSession`. The session owns one exact IFC STEP path/revision generation. The overload accepting an `IfcStandaloneDocument` first calls `ValidateGeneration`, so a stale/reopened document cannot be mixed into the publication.

## Publication contents

One `QuantBimSelectionExportBundle` contains the canonical selection identity, traceable quantity evidence, BOQ inventory, the existing standalone BOQ CSV, and evidence CSV. All projections come from the same session generation. Selection GUIDs are canonicalized deterministically and evidence validation remains fail-closed for missing GUIDs, duplicate document identities, missing geometry references, quantity/element identity mismatch, classification mismatch and non-finite quantities.

Before publication, the BOQ produced by the existing session QTO pipeline is compared with the BOQ rebuilt from traceable evidence. A disagreement fails closed instead of exporting mixed or contradictory commercial quantities.

## Architecture boundary

The implementation reuses `QuantBimStandaloneIfcSession` and `QuantBimTraceableTakeoffEngine`. It does not create a second IFC parser, QTO engine, classifier or BOQ engine. Reconstructed scene meshes and viewport hits remain visualization/navigation evidence only and do not become quantity authority. Shared Core has no BricsCAD, AutoCAD, WPF or WinForms dependency.

## Validation

Run `python scripts/preflight-quantbim-selection-export.py` and the `QS3D.Core.SmokeTests` project. Merge only after exact-head Shared + Hybrid CI are green, Reservation-v2/path collision remains clean, review threads are clear and the carrier is reconciled with latest protected `main`.
