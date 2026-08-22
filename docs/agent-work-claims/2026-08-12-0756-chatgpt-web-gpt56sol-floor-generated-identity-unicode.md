# Work claim — Floor generated identity Unicode integrity

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T07:56:00+07:00`
- Completed: `2026-08-12T08:00:00+07:00`
- Baseline main SHA: `8b75fa34ba5a58817e2a17adafa2a9a0e38fab8d`
- Priority: evidence-driven remote-safe generated-identity integrity

## Reason

`FloorGeneratedIdentityPlanner` built `LVO1:` owner tokens and `LVS1:` state tokens by hashing canonical strings with the default `Encoding.UTF8`. The default encoder replaces malformed UTF-16 such as unpaired surrogates instead of failing, so distinct malformed Floor ids or display names could collapse to replacement-character bytes before SHA-256.

## Changed scope

Floor generated identity id/name text must now be well-formed Unicode / strict-UTF8 encodable before token construction, and SHA-256 input uses the same strict UTF-8 encoder. Trimming, Floor-id uppercasing, length bounds, elevation formatting, token prefixes, SHA-256 format and valid Unicode behavior remain unchanged.

## Changed surfaces

- `src/QS3D.Core/Domain/FloorGeneratedIdentityPlanner.cs`
- `tests/QS3D.Core.SmokeTests/FloorGeneratedIdentityUnicodeSmoke.cs`
- this claim file

## Completion

- Claim commit: `6b7328080780a56dc147e90e0ddacd1cff8a0910`.
- Implementation commit: `959a32afc6c0a742ac20cc57496cd7918e6c06dc` — validate canonical Floor id/name text with strict UTF-8 and hash generated owner/state keys using that same encoder.
- Regression commit: `145bc4b80fe6465ae5527655859229970f327319` — cover malformed high/low surrogate ids, malformed display names, and valid supplementary-Unicode case/token determinism.
- Validation actually performed:
  - re-fetched current `FloorGeneratedIdentityPlanner` and confirmed strict UTF-8 validation plus strict SHA input encoding;
  - re-fetched the dedicated smoke and confirmed malformed + valid supplementary Unicode cases are covered;
  - no repository `dotnet` tests were executed in this hosted session;
  - no GitHub Actions were dispatched or rerun;
  - no BricsCAD runtime PASS is claimed.

## Coordination

Existing Floor generated identity work is older and focused stable ownership/state identity. Current floor-related claims cover vertical update/preflight/offset behavior and are disjoint from this identity-token file.

## Completion condition

Satisfied: current `main` fails closed on malformed Unicode before Floor owner/state SHA-256 hashing, valid Unicode remains deterministic, focused regression coverage is present, and this claim is released as `COMPLETED`.
