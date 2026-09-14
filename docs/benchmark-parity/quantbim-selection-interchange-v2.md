# QuantBIM standalone selection interchange V2

Version 2 adds a whole-envelope integrity boundary around the canonical version 1 selection interchange used by the Windows standalone IFC-QTO workflow.

## Why V2 exists

Version 1 already SHA-256 binds the BOQ CSV and evidence CSV payloads independently. Its document path, IFC revision, selection name and GUID membership are semantic metadata, however, and are not covered by one digest. Version 2 therefore wraps the exact canonical V1 text and hashes its exact UTF-8 bytes so metadata identity and payload identity travel together.

## Authority and architecture

`QuantBimSelectionExportPublisher` remains the quantity authority. `QuantBimSelectionInterchangeCodec` remains the V1 semantic codec. V2 does not parse IFC, calculate QTO, rebuild BOQ, infer geometry or promote renderer meshes into evidence. It only provides deterministic integrity framing around an already-published canonical V1 package.

The implementation lives in `QS3D.Core` and has no BricsCAD, AutoCAD, WPF or WinForms dependency. A standalone Windows host can persist or transmit V2 text without loading a CAD host.

## Format

The V2 text is canonical LF-only UTF-8 and contains exactly:

1. `QS3D-QUANTBIM-SELECTION-INTERCHANGE/2`
2. `EnvelopeSha256=<64 lowercase hex characters>`
3. `Payload=<base64 of exact canonical V1 UTF-8 bytes>`
4. one terminal LF

Decode validates the outer framing and digest before delegating to the V1 decoder. It then re-encodes the decoded package through V1 and requires byte-for-text equality with the wrapped payload. This rejects non-canonical V1 ordering or representation rather than silently normalizing it.

The outer V2 representation is canonical too. `Payload=` must be the exact `Convert.ToBase64String` spelling of the wrapped UTF-8 bytes; whitespace-tolerant or otherwise alternate base64 spellings are rejected instead of being normalized during import.

## Bounded standalone admission

Opening an interchange file is an untrusted standalone-workbench boundary, so V2 rejects oversized input before expensive decode stages. The current deterministic contract limits the complete encoded envelope to 16 Mi characters and the decoded canonical V1 payload to 12 MiB. The base64 field is length-checked before `Convert.FromBase64String`, the decoded byte count is checked before UTF-8 conversion, and encode applies the same payload/envelope limits before publication.

These limits protect the interchange boundary only; they do not change IFC parsing, QTO, BOQ, classification or evidence authority. A future format revision may define different limits, but V2 consumers must enforce the V2 limits consistently so the same package is either admitted or rejected across standalone hosts.

## Compatibility

V1 remains unchanged and independently decodable. V2 is an additive wrapper and deliberately uses a new version header instead of changing V1 semantics. Systems that only understand V1 can continue using V1 packages; systems requiring whole-envelope integrity should use V2.

The V2 wrapper is also deliberately transport-neutral: the same canonical text may be archived, attached to review evidence, or passed between standalone hosts without changing quantity or BOQ authority. Consumers must decode and validate the envelope before trusting its metadata or nested V1 payload.

Canonical V2 packages produced before bounded admission remain compatible when they are within the V2 limits and already use canonical base64. No existing V1 package semantics are changed.

## Evidence and security boundary

The SHA-256 digest is an integrity checksum, not a signature or trust assertion. It detects accidental or uncoordinated modification of document/revision/selection/CSV identity but does not authenticate who produced the package. Commercial quantity remains authoritative only through the generation-bound IFC session and traceable evidence pipeline that created the original export bundle.
