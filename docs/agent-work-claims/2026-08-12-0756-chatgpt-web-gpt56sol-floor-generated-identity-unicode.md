# Work claim — Floor generated identity Unicode integrity

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T07:56:00+07:00`
- Baseline main SHA: `8b75fa34ba5a58817e2a17adafa2a9a0e38fab8d`
- Priority: evidence-driven remote-safe generated-identity integrity

## Reason

`FloorGeneratedIdentityPlanner` builds `LVO1:` owner tokens and `LVS1:` state tokens by hashing canonical strings with the default `Encoding.UTF8`. The default encoder replaces malformed UTF-16 such as unpaired surrogates instead of failing. Distinct malformed Floor ids or display names can therefore collapse to replacement-character bytes before SHA-256, allowing lossy generated ownership/state identity instead of failing closed.

## Reserved scope

Require Floor generated identity id/name text to be well-formed Unicode / strict-UTF8 encodable before token construction, and hash with the same strict UTF-8 encoder. Preserve trimming, Floor-id uppercasing, length bounds, elevation formatting, token prefixes, SHA-256 format and valid Unicode behavior. Add focused CAD-independent regression coverage.

## Expected surfaces

- `src/QS3D.Core/Domain/FloorGeneratedIdentityPlanner.cs`
- `tests/QS3D.Core.SmokeTests/FloorGeneratedIdentityUnicodeSmoke.cs`
- this claim file

## Excluded scope

- No Floor CRUD, vertical placement, active-floor, UI/native or generated geometry behavior changes.
- No Unicode normalization/case-policy expansion for valid text; only malformed surrogate rejection.
- No token prefix/hash algorithm/state-key layout changes.
- No GitHub Actions dispatch and no BricsCAD runtime claim.

## Validation plan

- Assert malformed high/low surrogate Floor ids are rejected by `BuildOwnerToken()` rather than receiving replacement-fallback owner tokens.
- Assert malformed Floor display names are rejected by `Create()` before state-token publication.
- Assert valid supplementary Unicode represented by proper surrogate pairs remains accepted and deterministic; Floor-id case/order semantics remain unchanged.
- Re-fetch current source before write; never force-push.
- Record source/static verification only; do not claim an executed repository `dotnet` run in this hosted session.

## Coordination

Existing Floor generated identity work is older and focused stable ownership/state identity. Current floor-related claims cover vertical update/preflight/offset behavior and are disjoint from this identity-token file. No recent claim was found for malformed-Unicode hashing in `FloorGeneratedIdentityPlanner`.

## Completion condition

Current `main` fails closed on malformed Unicode before Floor owner/state SHA-256 hashing, valid Unicode remains deterministic, focused regression coverage is present, and this claim is marked `COMPLETED`.
