# Work claim — Material catalog Unicode integrity

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T01:08:00+07:00`
- Completed: `2026-08-12T01:12:00+07:00`
- Baseline main SHA: `0778ff7619cd36941fcdf050aae298e3400f28ff`
- Priority: evidence-driven remote-safe persistence integrity

## Reason

`ProjectMaterialCatalog` decodes stored custom-material fields with strict UTF-8 (`UTF8Encoding(false, true)`), but new `ProjectMaterial` text accepted malformed UTF-16 such as an unpaired surrogate. The write path used the default replacement UTF-8 encoder, so such a value could be accepted and then silently persisted as U+FFFD instead of round-tripping exactly. Rejecting malformed text only during write would also happen after `UpsertCustom` calls `ProjectState.Touch()`, so the safe boundary is material construction before project mutation.

## Reserved scope

Require `ProjectMaterial` id/name/unit/description text to be valid UTF-16/UTF-8-encodable text before it becomes catalog state. Preserve trimming, length bounds, built-in/custom uniqueness, Base64 record format, valid multilingual text and catalog ordering. Add focused CAD-independent regression coverage.

## Changed surfaces

- `src/QS3D.Core/Domain/ProjectMaterialCatalog.cs` (`ProjectMaterial` text validation only)
- `tests/QS3D.Core.SmokeTests/ProjectMaterialUnicodeIntegritySmoke.cs`
- this claim file

## Excluded scope

- No material naming/unit policy changes beyond malformed surrogate rejection.
- No changes to catalog record delimiter/Base64 format, rename/delete semantics, material schedule grouping, UI/native behavior or BricsCAD runtime.
- No GitHub Actions dispatch.

## Completion

- Implementation commit: `f9d944555965deed3049ef3b35891a15737302bb` — validate all `ProjectMaterial` text with strict UTF-8 byte counting and reject malformed surrogate input before it enters catalog state.
- Regression commit: `2d527e6853b1430fb182c46f22385d10226ac435` — cover unpaired high/low surrogate rejection, verify rejected upsert does not advance `ChangeVersion` or create catalog metadata, and verify a valid supplementary Unicode scalar round-trips exactly through catalog persistence.
- Final observed `main` before close: `42582bb6a84f14e2c64d037438448c25e58cdf9e`.
- Validation actually performed:
  - re-fetched current `ProjectMaterial` source and confirmed validation occurs in required/optional text normalization before `UpsertCustom` can call `Touch()`;
  - re-fetched the dedicated smoke source and confirmed malformed-input, no-partial-mutation and valid supplementary Unicode round-trip cases are present;
  - the first smoke create attempt hit a normal concurrent-main `409`; current head was re-fetched and the file was created without force;
  - no repository `dotnet` tests were executed in this hosted session;
  - no GitHub Actions were dispatched or rerun;
  - no BricsCAD runtime PASS is claimed.

## Coordination

Recent material-catalog work hardened decode/read integrity and duplicate-name handling. No current/recent claim was found for malformed UTF-16 input or replacement-fallback write corruption.

## Completion condition

Satisfied: current `main` rejects malformed material Unicode before project mutation, valid Unicode round-trips unchanged, focused regression coverage is present, and this claim is released as `COMPLETED`.
