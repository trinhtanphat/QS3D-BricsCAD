# Work claim — Wall junction ownership Unicode integrity

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T08:03:00+07:00`
- Baseline main SHA: `e391d9c2f44d48e6b66daa7e2e75736ed5eadd97`
- Priority: evidence-driven remote-safe ownership identity integrity

## Reason

`WallJunctionOwnershipPlanner` canonicalizes source-segment ids, wall element ids, project ids, and drawing fingerprints by trimming/uppercasing/length bounding, then hashes packed group/fingerprint keys with default `Encoding.UTF8`. Default UTF-8 replacement fallback silently maps malformed UTF-16 such as unpaired surrogates to replacement-character bytes. Distinct malformed ownership identities can therefore collapse before SHA-256 and receive lossy `WJP1:` group / `WJX1:` owner / `WJF1:` input-fingerprint identity instead of failing closed.

## Reserved scope

Require canonical wall-junction ownership identity text to be well-formed Unicode / strict-UTF8 encodable and hash with the same strict UTF-8 encoder. Preserve trimming, uppercasing, length bounds, packed key layout, numeric formatting, occurrence assignment, token prefixes, SHA-256 format, collision guards, planner limits and valid Unicode behavior. Add focused CAD-independent regression coverage.

## Expected surfaces

- `src/QS3D.Core/Geometry/WallJunctionOwnershipPlanner.cs` (canonical identity + SHA encoding only)
- `tests/QS3D.Core.SmokeTests/WallJunctionOwnershipUnicodeSmoke.cs`
- this claim file

## Excluded scope

- No junction geometry/classification/occurrence/vertical-overlap logic changes.
- No native materialization, UI, dependency, generated ownership schema or token-prefix/hash-algorithm changes.
- No broad Unicode normalization/culture-policy changes; only malformed surrogate rejection.
- No GitHub Actions dispatch and no BricsCAD runtime claim.

## Validation plan

- Assert malformed high/low surrogate project/drawing/wall/segment identity text is rejected before ownership token publication.
- Assert two distinct malformed surrogate identities are rejected rather than receiving replacement-fallback SHA tokens.
- Assert valid supplementary Unicode represented by proper surrogate pairs remains accepted and deterministic under existing case-insensitive canonicalization.
- Re-fetch current source before write; never force-push.
- Record source/static verification only; do not claim an executed repository `dotnet` run in this hosted session.

## Coordination

The latest wall-junction ownership claim is the completed bounded-enumeration lane from earlier today. The direct `WallJunction` result-snapshot lane is also completed and disjoint. No newer overlapping ownership-token claim was found before this reservation.

## Completion condition

Current `main` fails closed on malformed Unicode before `WJP1/WJX1/WJF1` SHA-256 hashing, valid Unicode remains deterministic, focused regression coverage is present, and this claim is marked `COMPLETED`.
