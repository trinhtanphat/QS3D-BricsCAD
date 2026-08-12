# Work claim — Generated stale handle numeric identity

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-generated-stale-handle-identity`
- Registered: `2026-08-12T13:27:00+07:00`
- Last Updated: `2026-08-12T13:27:00+07:00`
- Baseline main SHA: `34637af83161a538d9cb2af81ea5a86ac6f41022`
- Priority: evidence-driven generated-output freshness defect found during owner-requested `continue all`
- Task Key: `GENERATED-STALE-HANDLE-NUMERIC-IDENTITY`

## Confirmed defect

Generated CAD ownership now compares handle spelling by numeric hexadecimal identity, so values such as `A`, `0A`, and `0xA` identify the same native CAD object. `ProjectElement` generated-output stale snapshots still build signatures from trimmed text only. After an output is marked stale, a spelling-only metadata rewrite from `0A` to `A` therefore makes `IsGeneratedSolidStale()` / related stale queries return false even though the generated native output was not rebuilt or replaced.

## Reserved scope

Use one shared Core handle-identity normalizer for generated ownership and `ProjectElement` stale-output signatures. Preserve current fallback behavior for malformed/non-positive tokens, current semicolon-list ordering/deduplication semantics, query purity, stale-marker lifecycle, and all per-kind stale snapshot keys.

## Expected surfaces

- `src/QS3D.Core/Domain/ProjectElement.cs`
- a low-level shared Core handle identity helper
- `src/QS3D.Core/Diagnostics/GeneratedHandleOwnershipPolicy.cs` only to delegate existing normalization behavior to the shared helper
- `tests/QS3D.Core.SmokeTests/GeneratedGeometryStaleSmoke.cs`
- this claim file

## Explicit exclusions / coordination

- Do not change `BomReleaseGuardService`; its numeric-handle lane is already completed.
- Do not change generated-handle persistence spelling policy, health canonicality warnings, owner-slot semantics, native live-handle adapters, UI/native/release surfaces, or the accepted numeric domain of handles in this lane.
- Do not alter stale query purity or explicit stale-clear semantics.
- No GitHub Actions/build/release dispatch and no BricsCAD V25/V26 runtime qualification.

## Validation plan

- A generated output marked stale with `0A` remains stale after its stored handle spelling changes to `A` or `0xA`.
- A genuinely different handle such as `B` still makes the old stale snapshot obsolete.
- Multi-handle signatures remain order-independent and duplicate-insensitive under numeric-equivalent spellings.
- Existing generated ownership normalization behavior remains unchanged by delegation.
- Re-fetch moving `main` target blobs and inspect exact PR diff before integration.

## Completion condition

Current `main` uses the same numeric handle identity contract for generated ownership and generated stale signatures, focused regression source is merged, and this claim is closed `COMPLETED` with exact evidence.
