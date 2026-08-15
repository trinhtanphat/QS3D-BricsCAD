# Work claim — Interchange provenance target-map Unicode integrity

- Status: `ACTIVE`
- Agent: `audit-interchange-gap-next-20260815-r3`
- Registered: `2026-08-15T10:50:13+07:00`
- Baseline main SHA: `88f83db19ed5dfd85606d5a5e00adfc28f4fd99c`
- Related issue: `#84`
- Priority: remote-safe interchange provenance correctness
- Claim branch: `agent/audit-interchange-gap-next/issue84-target-map-unicode-claim-20260815`

## Confirmed defect

`ProjectInterchangeProvenanceTargetMap.DecodeRecord` rejects invalid persisted UTF-8 through the existing strict encoder, but `Token` and `EncodeRecord` still write through replacement-fallback `Encoding.UTF8.GetBytes`. The public `Store` boundary accepts a lone UTF-16 surrogate in source project identity, source drawing fingerprint, or source Element identity, then silently persists U+FFFD. This can make stored provenance differ from the reviewed in-memory identity and can produce a target-map record that no longer matches the caller identity during readback.

## Reserved scope

- `src/QS3D.Core/Export/ProjectInterchangeProvenanceTargetMap.cs` — reuse its existing strict UTF-8 encoder in both token and record serialization, preserving all current normalization, case, size, cardinality, target-resolution, rollback, and strict read contracts. Record construction remains before target metadata, audit, or revision mutation.
- `tests/QS3D.Core.SmokeTests/ProjectInterchangeProvenanceTargetMapUnicodeIntegritySmoke.cs` — one self-registering deterministic regression for lone high/low surrogate rejection, no partial metadata/audit/revision mutation, and exact supplementary-Unicode Store/readback/record preservation.
- `scripts/preflight-interchange-provenance-target-map-unicode.py` — one focused auto-discovered source guard.
- this claim file for coordination and handoff evidence.

## Explicit exclusions

- No `ProjectInterchangeSourceHandleProvenance`, semantic snapshot/import/merge/remap policy, ProjectState/domain, target-DWG handle adoption, IFC, or BCF changes.
- No native BricsCAD adapter/runtime, LOCAL probe/runner, private data, release/signing, workflow, or GitHub Actions work.
- No Unicode normalization, case, trimming, identity length, mapping cardinality, encoded-size, audit wording, or issue-state policy changes; broad issue `#84` remains open.

## Coordination evidence

At baseline `88f83db19ed5dfd85606d5a5e00adfc28f4fd99c`, current issue `#84`, source/history, open PR file lists, remote branches, and relevant ACTIVE/BLOCKED claims were inspected. The only target-map claims are completed read-side strict-UTF8 and padded-ID lanes. Open interchange work owns BCF/XML or other files. No competing open PR or ACTIVE/BLOCKED claim owns the exact target-map writer/test/gate surfaces.

## Validation plan

- focused target-map Unicode preflight plus relevant interchange provenance/validation guards;
- QS3D.Core and Core-smoke Release builds;
- full deterministic Core smoke;
- aggregate discovered feature preflights, recording unrelated failures without expanding scope;
- refresh `origin/main`, re-audit exact PR/claim collisions and final diff before every publication, push normally, and open an implementation PR without merging `main`.

## Completion condition

Malformed UTF-16 fails before target metadata/audit/revision mutation; valid supplementary Unicode remains ordinally identical through token/record storage and public readback; all existing target-map contracts remain intact; remote-safe validation is recorded; and the implementation is handed off in an unmerged PR while broad issue `#84` remains open.
