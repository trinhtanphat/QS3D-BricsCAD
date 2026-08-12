# Rebar Schedule XLSX export — active-DWG ownership

Date: 2026-08-11  
Scope: `RebarScheduleWindow` modeless BBS export only

## Problem

`RebarScheduleWindow` is bound to the DWG that opened it, but its XLSX export path previously showed `SaveFileDialog` before validating that this DWG was still the active BricsCAD document. Locate already failed closed on document ownership, while schedule export could ask the user to select/confirm a file path first and reject only afterward.

That ordering is undesirable for a modeless document-bound window and is inconsistent with the other current schedule windows.

## Implementation

`OnExportClick` now uses two ownership gates around the modal save boundary:

1. call `EnsureActive("xuất BBS XLSX")` before constructing/showing `SaveFileDialog`;
2. after the dialog returns successfully, call the same guard again before resolving the current semantic rows;
3. rebuild rows from `ProjectContextCoordinator.TryGetReadOnly` through a detached `ProjectStateSnapshot` and preview regeneration;
4. export only those rebuilt current rows.

The second guard is intentionally retained. The save dialog is a user-controlled/modal boundary; re-checking afterward avoids assuming document ownership stayed unchanged while that UI was open.

## Invariants preserved

- The window remains attached through `DocumentBoundWindowLifetime`.
- Locate still checks active-DWG ownership and re-resolves the selected BBS row against the current read-only project before locating it.
- `BuildCurrentRows` still creates a detached project snapshot and preview-regenerates only that snapshot.
- Export does not create a replacement project and does not mutate the live project merely to refresh the BBS.
- `XlsxRebarScheduleExporter` behavior and file format are unchanged.

## Regression gate

`scripts/preflight-rebar-schedule-export-active-guard.py` checks the source ordering:

`pre-dialog EnsureActive -> SaveFileDialog -> post-dialog EnsureActive -> BuildCurrentRows -> XLSX export`.

It also protects the existing Locate ownership ordering and detached/read-only current-row rebuild contract.

## Validation boundary

This change was made through the GitHub source connector in a remote environment. GitHub Actions were not dispatched. A native BricsCAD V25 UI/runtime PASS is not claimed here; native modeless/UI qualification remains part of the existing local validation queue.
