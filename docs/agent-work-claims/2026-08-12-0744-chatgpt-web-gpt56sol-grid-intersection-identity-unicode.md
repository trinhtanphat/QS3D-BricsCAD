# Work claim — Grid intersection identity Unicode integrity

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T07:44:00+07:00`
- Completed: `2026-08-12T07:47:00+07:00`
- Baseline main SHA: `f1764a3329a8f70eaf973b8566c02a81eb12b9d3`
- Priority: evidence-driven remote-safe identity correctness

## Reason

`GridIntersectionIdentityPlanner.BuildPairToken()` hashed a canonical pair key with replacement-fallback UTF-8. Distinct malformed UTF-16 Grid ids could therefore have different pair keys but encode through the same replacement character bytes and receive the same SHA-256 pair token from the public token builder. Identity input now fails closed before hashing.

## Changed scope

Require canonical Grid element ids used by intersection identity to be well-formed Unicode / strict-UTF8 encodable before pair-key/token construction. Preserve trimming, case normalization, length bounds, pair ordering, SHA-256 format, occurrence assignment, collision guard and public token format for valid text.

## Changed surfaces

- `src/QS3D.Core/Geometry/GridIntersectionIdentityPlanner.cs`
- `tests/QS3D.Core.SmokeTests/GridIntersectionIdentityUnicodeSmoke.cs`
- this claim file

## Excluded scope

- No Grid geometry/intersection math, marker ownership format, SHA algorithm, pair-token prefix or occurrence semantics changes.
- No broad Unicode normalization/culture policy; only malformed surrogate rejection.
- No native/UI changes, GitHub Actions dispatch or BricsCAD runtime claim.

## Completion

- Claim commit: `ddae2c7de09f76c1058083ed3de7b242075dccb9`.
- Implementation commit: `d2782d8838f2cd622edd10eafe396b7ebfecc20b` — validate canonical ids with strict UTF-8 and use the same strict encoder for SHA input.
- Regression commit: `6ba5a7666345c4fad6fe76441a16d3e13d453792` — cover malformed high/low surrogate rejection through both token building and assignment, plus valid supplementary Unicode pair-token/assignment determinism.
- Validation actually performed:
  - re-fetched current canonical-id/token source and confirmed strict UTF-8 validation/encoding is present;
  - re-fetched the dedicated smoke and confirmed malformed + valid supplementary Unicode cases are covered;
  - no repository `dotnet` tests were executed in this hosted session;
  - no GitHub Actions were dispatched or rerun;
  - no BricsCAD runtime PASS is claimed.

## Coordination

Recent Grid intersection identity work covered deterministic pair ownership and bounded enumeration. No overlapping current claim was found for malformed-Unicode identity hashing.

## Completion condition

Satisfied: current `main` cannot hash malformed Grid identity text through UTF-8 replacement fallback, focused regression coverage is present, and this claim is released as `COMPLETED`.
