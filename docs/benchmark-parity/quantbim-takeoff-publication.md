# QuantBIM standalone takeoff publication

## Scope

This P1 slice adds a host-neutral publication boundary for the QuantBIM standalone IFC-QTO workflow. It turns the current IFC document generation, named selection, selected-element evidence and aggregated BOQ into one detached snapshot that can be exported or handed to another process without silently reading mutable model state again.

The implementation stays in `QS3D.Core`. It does not introduce a BricsCAD dependency, renderer, Windows UI toolkit or licensed CAD runtime requirement. The concrete standalone Windows shell remains outside this shared-Core boundary and may consume these contracts.

## Generation fence

`QuantBimTakeoffPublisher.Publish` requires the caller's expected IFC revision/fingerprint and compares it exactly with the current `IfcStandaloneDocument.Revision`. Publication fails closed when they differ. `ValidateCurrent` additionally verifies both the document path and exact revision before a previously published snapshot is reused.

This prevents a same-path replacement IFC from receiving BOQ/evidence created from an earlier generation.

## Evidence identity

Before quantity aggregation, the publisher builds a case-insensitive GUID index and rejects duplicate IFC identities. Every selected GUID must exist in that exact document generation. Evidence rows retain:

- IFC GUID;
- entity;
- storey;
- type;
- classification;
- geometry/source reference.

Rows are deterministically ordered by GUID. The existing `IfcQtoWorkbench` remains the canonical quantity-to-BOQ aggregation engine, so this boundary does not fork quantity semantics.

## BOQ publication

Published BOQ rows contain classification, canonical unit, finite quantity and source count. Ordering is deterministic across classification and unit. Numeric serialization uses invariant culture and round-trip floating-point formatting.

## Interchange

`QuantBimTakeoffPublicationCodec` emits version `QS3D-QUANTBIM-TAKEOFF-PUBLICATION/1`. Text fields are UTF-8/base64 framed so Windows paths, Unicode, delimiters and line breaks cannot become structural ambiguity. Evidence records must precede BOQ records; malformed keys, malformed base64 and malformed numeric values are rejected rather than partially accepted.

The codec is intentionally transport-oriented rather than tied to a filesystem or UI. A standalone Windows workbench can save it beside an IFC, attach it to a review package, or translate it into a richer external format without pulling BricsCAD types into Core.

## Verification

`QsQuantBimTakeoffPublicationSmoke` is module-initialized and therefore runs automatically with the deterministic smoke assembly. It verifies revision fencing, duplicate/missing GUID rejection, Unicode-safe deterministic roundtrip, finite BOQ output and validation against a replacement IFC generation. `preflight-quantbim-takeoff-publication.py` guards the architecture and safety invariants in CI.
