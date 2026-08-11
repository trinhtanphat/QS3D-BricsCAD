# Work claim — Material catalog Unicode integrity

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T01:08:00+07:00`
- Baseline main SHA: `0778ff7619cd36941fcdf050aae298e3400f28ff`
- Priority: evidence-driven remote-safe persistence integrity

## Reason

`ProjectMaterialCatalog` decodes stored custom-material fields with strict UTF-8 (`UTF8Encoding(false, true)`), but new `ProjectMaterial` text currently accepts malformed UTF-16 such as an unpaired surrogate. The write path uses the default replacement UTF-8 encoder, so such a value can be accepted and then silently persisted as U+FFFD instead of round-tripping exactly. Rejecting malformed text only during write would also occur after `UpsertCustom` calls `ProjectState.Touch()`, so the safe boundary is material construction before project mutation.

## Reserved scope

Require `ProjectMaterial` id/name/unit/description text to be valid UTF-16/UTF-8-encodable text before it becomes catalog state. Preserve trimming, length bounds, built-in/custom uniqueness, Base64 record format, valid multilingual text and catalog ordering. Add focused CAD-independent regression coverage.

## Expected surfaces

- `src/QS3D.Core/Domain/ProjectMaterialCatalog.cs` (`ProjectMaterial` text validation only)
- `tests/QS3D.Core.SmokeTests/ProjectMaterialUnicodeIntegritySmoke.cs`
- this claim file

## Excluded scope

- No material naming/unit policy changes beyond malformed surrogate rejection.
- No changes to catalog record delimiter/Base64 format, rename/delete semantics, material schedule grouping, UI/native behavior or BricsCAD runtime.
- No GitHub Actions dispatch.

## Validation plan

- Assert unpaired high/low surrogates are rejected in material text at construction/upsert before metadata persistence.
- Assert a valid supplementary Unicode scalar represented by a proper surrogate pair is accepted and round-trips through `UpsertCustom` + `GetCustom` unchanged.
- Assert a rejected upsert does not create catalog metadata or advance `ProjectState.ChangeVersion`.
- Re-fetch current source blob before write; never force-push.
- Record source/static verification only; do not claim an executed repository `dotnet` run in this hosted session.

## Coordination

Recent material-catalog work hardened decode/read integrity and duplicate-name handling. No current/recent claim was found for malformed UTF-16 input or replacement-fallback write corruption.

## Completion condition

Current `main` rejects malformed material Unicode before project mutation, valid Unicode round-trips unchanged, focused regression coverage is present, and this claim is marked `COMPLETED`.
