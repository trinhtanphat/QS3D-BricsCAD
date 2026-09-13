# QuantBIM standalone IFC session generation fence

## Purpose

`QuantBimStandaloneIfcSession` binds the standalone IFC document, workbench operations and scene geometry to the same STEP payload. This prevents a host from parsing properties/QTO from one IFC generation while resolving 3D geometry from another generation.

The session reuses the existing `IfcStepStandaloneSource`, `IfcStepGeometryResolver`, `QuantBimStandaloneWorkbench` and `QuantBimStandaloneSceneBuilder`. It does not introduce a second IFC parser, quantity engine, BOQ aggregator or scene implementation.

## Contract

`Parse(path, stepText)` constructs both the semantic/QTO document and geometry resolver from the exact same STEP payload. `Open(path)` reads the file once and delegates to that same boundary. The bound document path and revision are then exposed by the session.

Filtering, property inspection, selection takeoff, BOQ construction, CSV export and scene construction always operate on the bound document. An externally supplied document may be used only as a generation assertion: its path must match case-insensitively and its revision/fingerprint must match exactly. After validation, scene publication still uses the session-owned document rather than trusting externally supplied element rows.

A stale revision, different path, unknown selection GUID or geometry resolution failure is fail-closed. Callers should create a new session after the IFC source changes rather than reusing a resolver or document from a previous generation.

## Quantity/evidence boundary

QTO remains authoritative for quantities. Scene mesh vertices and triangle topology are visualization/navigation evidence only and are never promoted into takeoff quantities, BOQ values, estimate quantities or procurement quantities.

The session does not change existing `IfcQtoWorkbench` aggregation semantics or publication interchange. It only guarantees that standalone properties/QTO and scene geometry cannot silently drift across IFC generations when the session API is used.

## Architecture boundary

This type lives in `QS3D.Core` and has no BricsCAD, AutoCAD, WinForms or renderer dependency. A Windows standalone host can own the session directly, while a BricsCAD adapter remains optional and may consume the same Core contract without becoming required by it.

The bounded IFC STEP geometry subset is still defined by `IfcStepGeometryResolver`; unsupported geometry continues to fail closed. Extending tessellation/profile support is a separate parity lane from generation coherence.

## Compatibility

The API is additive. Existing direct users of `IfcStepStandaloneSource`, `QuantBimStandaloneWorkbench` or `QuantBimStandaloneSceneBuilder` remain source-compatible, but standalone hosts that need verifiable same-generation scene/QTO behavior should prefer `QuantBimStandaloneIfcSession`.
