# Work claim — Grid intersection identity Unicode integrity

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T07:44:00+07:00`
- Baseline main SHA: `f1764a3329a8f70eaf973b8566c02a81eb12b9d3`
- Priority: evidence-driven remote-safe identity correctness

## Reason

`GridIntersectionIdentityPlanner.BuildPairToken()` hashes a canonical pair key with `Encoding.UTF8.GetBytes`. The default UTF-8 encoder replaces malformed UTF-16 (for example, an unpaired surrogate) with the Unicode replacement character instead of failing. Consequently distinct semantic Grid ids containing different malformed surrogate code units can produce different pair keys but the same encoded bytes and SHA-256 pair token. `Assign()` has a later collision guard, but the public `BuildPairToken()` API can still return the same ownership token for distinct malformed identities. Identity inputs should fail closed before hashing rather than be lossy-normalized by the encoder.

## Reserved scope

Require canonical Grid element ids used by intersection identity to be well-formed Unicode / strict-UTF8 encodable before pair-key/token construction. Preserve trimming, case normalization, length bounds, pair ordering, SHA-256 format, occurrence assignment, collision guard and public token format for valid text. Add focused CAD-independent regression coverage.

## Expected surfaces

- `src/QS3D.Core/Geometry/GridIntersectionIdentityPlanner.cs` (canonical id Unicode validation only)
- `tests/QS3D.Core.SmokeTests/GridIntersectionIdentityUnicodeSmoke.cs`
- this claim file

## Excluded scope

- No Grid geometry/intersection math, marker ownership format, SHA algorithm, pair-token prefix or occurrence semantics changes.
- No broad Unicode normalization/culture policy; only malformed surrogate rejection.
- No native/UI changes, GitHub Actions dispatch or BricsCAD runtime claim.

## Validation plan

- Assert distinct malformed high/low surrogate ids are rejected by `BuildPairToken()` rather than receiving replacement-fallback tokens.
- Assert `Assign()` rejects malformed Grid ids before identity publication.
- Assert valid supplementary Unicode represented by a proper surrogate pair remains accepted and produces deterministic case/order-insensitive pair tokens.
- Re-fetch current source blob before write; never force-push.
- Record source/static verification only; do not claim an executed repository `dotnet` run in this hosted session.

## Coordination

Recent Grid intersection identity work covered deterministic pair ownership and bounded enumeration. No current/recent claim was found for malformed-Unicode/replacement-fallback identity hashing.

## Completion condition

Current `main` cannot hash malformed Grid identity text through UTF-8 replacement fallback, focused regression coverage is present, and this claim is marked `COMPLETED`.
