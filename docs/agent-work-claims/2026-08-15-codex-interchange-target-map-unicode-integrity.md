# Work claim — Interchange provenance target-map Unicode integrity

- Status: `COMPLETED`
- Agent: `audit-interchange-gap-next-20260815-r3`
- Registered: `2026-08-15T10:50:13+07:00`
- Completed: `2026-08-15T11:07:47+07:00`
- Baseline main SHA: `88f83db19ed5dfd85606d5a5e00adfc28f4fd99c`
- Related issue: `#84`
- Priority: remote-safe interchange provenance correctness
- Claim branch: `agent/audit-interchange-gap-next/issue84-target-map-unicode-claim-20260815`
- Claim PR / commit / merge: `#1561` / `d2c1cc9cfb368095a1e49990a97d7dc3f0f9757a` / `c5fbe4af9fb98383679f279e33d9b93eb2ec737d`
- Implementation branch: `agent/audit-interchange-gap-next/issue84-target-map-unicode-impl-20260815`
- Implementation PR / source / merge: `#1567` / `63b5183abc2f983a0d826001ae70ad79b84654a2` / `6b686a32934ef9fd750f3ff5ade6508cc14259c9`

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

Malformed UTF-16 fails before target metadata/audit/revision mutation; valid supplementary Unicode remains ordinally identical through token/record storage and public readback; all existing target-map contracts remain intact; remote-safe validation is recorded; and the implementation is represented in current `main` while broad issue `#84` remains open.

## Completion evidence

- Claim-first reservation commit `d2c1cc9cfb368095a1e49990a97d7dc3f0f9757a` reached `main` through PR `#1561` at `c5fbe4af9fb98383679f279e33d9b93eb2ec737d` before implementation edits.
- Implementation commit `63b5183abc2f983a0d826001ae70ad79b84654a2` changed only the target-map writer plus one new self-registering Unicode smoke and one focused auto-discovered preflight. PR `#1567` merged normally at exact main SHA `6b686a32934ef9fd750f3ff5ade6508cc14259c9`.
- `Token` and `EncodeRecord` now reuse the existing `UTF8Encoding(false, true)` instance. Strict token/record construction remains before `ProjectStateSnapshot` capture and every target metadata removal/write, audit append, and `Touch`, so malformed text cannot partially mutate canonical target state.
- Regression coverage proves lone high/low surrogate rejection across source project identity, drawing fingerprint, and source Element identity; rejected writes preserve metadata, audit count, and `ChangeVersion`; valid supplementary Unicode remains ordinally identical through result identity, project/element record decoding, and public target-id readback.
- On exact implementation commit `63b5183abc2f983a0d826001ae70ad79b84654a2`: focused target-map Unicode, remap/source provenance, remap append, interchange validation, smoke registration, and repository gates passed; QS3D.Core and Core-smoke Release builds completed with zero warnings/errors; full deterministic Core smoke reported `ALL PASS`; all `817/817` discovered feature gates passed.
- The integration coordinator independently revalidated exact merge `6b686a32934ef9fd750f3ff5ade6508cc14259c9`: target-map Unicode, remap provenance, provenance lifecycle, and smoke-registration gates passed; both Release builds completed with zero warnings/errors; full Core smoke reported `ALL PASS`.
- Issue comment handoff: `https://github.com/trinhtanphat/QS3D-BricsCAD/issues/84#issuecomment-5300427567`. Broad issue `#84` remains open for native/runtime/format/policy scope. No GitHub Actions, BricsCAD/native/LOCAL runtime, release/workflow, or private data was used.
