# QuantBIM standalone selection interchange

This boundary packages one already-published `QuantBimSelectionExportBundle` into a deterministic, renderer-neutral interchange envelope for Windows standalone hosts.

## Authority and architecture

`QuantBimSelectionExportPublisher` remains the quantity authority. The interchange codec does not parse IFC, calculate quantities, rebuild BOQ, or treat scene meshes as commercial evidence. It only packages the exact-generation document identity, selection identity, BOQ CSV and traceable evidence CSV already produced by the canonical standalone session/evidence pipeline.

The contract lives in `QS3D.Core` and has no BricsCAD, AutoCAD, WPF or WinForms dependency. A Windows desktop shell may persist, upload, attach, or transmit the encoded text without introducing a CAD-host dependency into Core.

## Format

Version 1 begins with `QS3D-QUANTBIM-SELECTION-INTERCHANGE/1` and uses canonical LF line endings. Metadata and CSV payloads are strict UTF-8 encoded and base64-wrapped. Selection GUIDs are deduplicated case-insensitively and sorted deterministically.

The envelope binds:

- IFC document path and revision;
- selection name and canonical GUID membership;
- BOQ CSV payload;
- evidence CSV payload;
- SHA-256 of the exact UTF-8 bytes for each CSV payload.

Decode is fail-closed. It rejects unsupported headers, CR/CRLF input, missing terminal newline, malformed field ordering/cardinality, invalid base64 or UTF-8, invalid SHA-256 text, duplicate GUID identity, and any payload/hash mismatch.

## Compatibility

The versioned header is the compatibility boundary. Future formats must use a new header rather than silently changing version 1 semantics. Version 1 is text-only and therefore suitable for filesystem persistence, clipboard-safe transport after normal text handling, API payloads, support attachments and interchange tests.

## Evidence rule

The SHA-256 values protect package integrity; they do not promote the package or scene geometry into a new quantity authority. Commercial quantity remains derived from the bound IFC session and traceable takeoff evidence that produced the bundle.