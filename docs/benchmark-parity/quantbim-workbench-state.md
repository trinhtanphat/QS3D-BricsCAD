# QuantBIM standalone workbench state

## Scope

`QS3D.Core` exposes host-neutral IFC/QTO, evidence, scene and viewport-orchestration contracts that may be consumed by a Windows host. This slice adds a persistence boundary for the user-visible workbench context without adding a standalone executable, renderer, CAD engine or BricsCAD dependency.

The concrete standalone desktop shell remains the responsibility of sibling `QS3D-CAD` under `docs/PRODUCT-BOUNDARY.md`.

## Persisted generation identity

A `QuantBimWorkbenchStateSnapshot` records:

- IFC document path;
- exact document revision/fingerprint;
- entity, storey, type and classification filters;
- named selection-set identity;
- deterministically ordered selected IFC GUIDs.

Restore is fail-closed. The current document path must match case-insensitively and the revision must match exactly. Reusing the same pathname for replacement IFC bytes therefore cannot silently revive selection from an earlier document generation.

## Selection and filter invariants

Capture and restore validate the current document before state can be published:

- duplicate IFC GUIDs are rejected;
- every selected GUID must exist in the current document;
- every selected element must remain visible under the active filter;
- selected GUIDs are de-duplicated case-insensitively and emitted deterministically.

These checks reuse existing `IfcWorkbenchFilter`, `IfcSelectionSet` and `IfcStandaloneDocument` contracts rather than creating another filter or selection engine.

## Transport format

`QuantBimWorkbenchStateCodec` provides a deterministic, versioned UTF-8 text representation. The format begins with `QS3D-QUANTBIM-WORKBENCH-STATE/1`; fixed fields and selected GUIDs are base64 encoded so paths, Unicode names, delimiters and line breaks cannot become structural ambiguity.

The decoder requires the exact field order and rejects malformed headers, unexpected fields, unexpected blank lines and invalid base64. Hosts may store this text in their own settings/session storage; Core does not prescribe filesystem or UI persistence.

## Compatibility

Existing `QuantBimStandaloneWorkbench`, IFC STEP ingestion, traceable takeoff, BOQ aggregation, CSV export, scene and viewport-host APIs remain unchanged. BricsCAD-specific adapters stay optional and no BricsCAD type crosses this shared-Core boundary.

Hosted Core tests prove deterministic state semantics only. They do not claim native Windows UI, standalone renderer, packaging or licensed BricsCAD runtime qualification.
