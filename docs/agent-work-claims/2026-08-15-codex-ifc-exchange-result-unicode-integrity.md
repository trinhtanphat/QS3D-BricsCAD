# Work claim — IFC exchange-result Unicode integrity

- Status: `COMPLETED`
- Agent: `codex-audit-interchange-gap-20260815`
- Registered: `2026-08-15T00:00:00+07:00`
- Completed: `2026-08-15T08:39:00+07:00`
- Baseline main SHA: `b0fe2bb88206ddab1cb99ae1c1154838f6eaa6b3`
- Related issue: `#84`
- Priority: remote-safe IFC interoperability correctness

## Confirmed defect

`IfcRoundTripExchangeResult` describes its public identity/evidence strings as canonical tokens, but its private token validator checks only blank text, surrounding whitespace, and control characters. A lone UTF-16 high or low surrogate is none of those and is therefore accepted as an external object, classification, mapping, cost-relation, or state-detail token. Such malformed text cannot be faithfully encoded at a real interchange boundary, so the result envelope can currently publish invalid canonical evidence instead of failing closed.

## Reserved scope

- `src/QS3D.Core/Export/IfcRoundTripExchangeResult.cs` — reject unpaired UTF-16 surrogates in the existing canonical-token validation while preserving every current identity/state/relation rule.
- `tests/QS3D.Core.SmokeTests/IfcRoundTripExchangeResultUnicodeIntegritySmoke.cs` — one self-registering regression proving lone high/low rejection and exact valid supplementary-Unicode preservation.
- this claim file for completion evidence.

## Explicit exclusions

- No BCF source/tests/package behavior owned by the active BCF claim.
- No `IfcRoundTripProjection`, quantity-evidence, parser/writer, IFC schema/library, native BricsCAD adapter/runtime, LOCAL probe/runner, private data, release/signing, or GitHub Actions work.
- No broader import policy, Unicode normalization, identifier case policy, resource-bound redesign, or issue `#84` closure.

## Coordination evidence

At baseline `b0fe2bb88206ddab1cb99ae1c1154838f6eaa6b3`, current source, issue `#84`, all ACTIVE/BLOCKED claims, recent history, and open PRs were inspected. No current owner or open PR reserves this exact file/contract. The active BCF claim owns `BcfIssueExchange*` / `BcfZipPackage*`; the active FieldMerge claim owns the coordinator surfaces; both are excluded here.

## Validation plan

- focused self-registering smoke plus existing IFC smokes;
- QS3D.Core Release build and full Core smoke;
- relevant IFC/interchange preflights and aggregate source guards when practical;
- final current-main reconciliation, exact diff review, PR merge/readback, and truthful exact-SHA evidence.

## Completion condition

The result envelope rejects malformed UTF-16 before exposing canonical evidence, valid supplementary Unicode remains byte-for-byte equivalent at the managed-string boundary, focused and full remote-safe validation passes, the implementation and claim closeout are merged, and issue `#84` remains open for its broader format/native/runtime scope.

## Completion evidence

- Claim-only PR `#1439` merged as `4a8866c19b8fe2aa501e2db30da5ae072d55ec30` before implementation edits.
- Source implementation commit: `a06f2c232327273a0170cbfffe621135a51abffb`.
- Implementation PR `#1449` squash-merged to `main` as `4d91901156ba01710c1ee62bf7ba75892def218c` with exactly the reserved production file and new self-registering smoke.
- Exact merged SHA `4d91901156ba01710c1ee62bf7ba75892def218c` passed QS3D.Core and Core-smoke Release builds with `0 warnings / 0 errors`, full Core smoke `ALL PASS`, all `49` interchange preflights, and all `808` discovered aggregate feature preflights.
- Lone high and low surrogates now fail at the result canonical-token boundary; valid supplementary Unicode remains ordinally identical in external identity, state detail, and classification evidence.
- No GitHub Actions were manually dispatched or otherwise operated. No BCF, FieldMerge, native/runtime, LOCAL, private-data, release, or signing surface was changed.
- Issue `#84` deliberately remains open; this claim closes only the bounded IFC exchange-result Unicode contract.
