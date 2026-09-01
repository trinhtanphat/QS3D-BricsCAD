# TBQ workspace persistence encoding bound

## Scope

This carrier makes the existing 1 MiB-character TBQ project-workspace persistence ceiling an incremental encode-time contract rather than a post-materialization check.

## Defect

`ProjectTbqWorkspaceCodec.Value` previously appended every encoded field to an unbounded `StringBuilder`, materialized the full string, and only then rejected payloads larger than `MaxPayloadChars`. An oversized public TBQ workspace could therefore be copied into persistence output beyond the declared bound before deterministic failure.

## Production contract

`AppendField` computes the exact prospective encoded size using UTF-16 `string.Length`, the canonical decimal length-token width, the separator colon, and the field value. If the next field would exceed `MaxPayloadChars`, encoding fails before the builder is mutated.

The wire format remains `<length>:<value>` and the existing 1 MiB-character limit is unchanged. Exact-fit payload prefixes remain allowed. Supplementary Unicode scalars continue to count as two UTF-16 code units, matching the decoder and .NET string contract. Final XML validation and self-decode validation remain mandatory.

## Deterministic coverage

`TbqProjectWorkspaceEncodingBoundSmoke` verifies:

- exact-boundary append with supplementary Unicode;
- length-prefix digits and colon participate in the bound;
- first-over-limit append rejects before changing builder length;
- a late overflow preserves the already accepted prefix exactly.

Focused auto-discovered guard: `python scripts/preflight-tbq-workspace-encoding-bound.py`.

Runtime classification: `NOT_APPLICABLE`; this is deterministic Core/commercial persistence correctness and does not claim licensed BricsCAD runtime evidence.
