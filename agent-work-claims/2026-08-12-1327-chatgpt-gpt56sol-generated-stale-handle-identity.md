# Work claim — Generated stale handle numeric identity

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-gpt56sol-20260812-generated-stale-handle-identity`
- Registered: `2026-08-12T13:27:00+07:00`
- Last Updated: `2026-08-12T13:41:00+07:00`
- Baseline main SHA: `34637af83161a538d9cb2af81ea5a86ac6f41022`
- Priority: evidence-driven generated-output freshness defect found during owner-requested `continue all`
- Task Key: `GENERATED-STALE-HANDLE-NUMERIC-IDENTITY`

## Confirmed defect

Generated CAD ownership compares handle spelling by numeric hexadecimal identity, so values such as `A`, `0A`, and `0xA` identify the same native CAD object. `ProjectElement` generated-output stale snapshots still built signatures from trimmed text only. After an output was marked stale, a spelling-only metadata rewrite could therefore make a stale generated output appear fresh even though the native output was not rebuilt or replaced.

## Completed implementation

- Added low-level `GeneratedHandleIdentity` with the exact pre-existing ownership normalization behavior.
- Kept `GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(...)` public behavior/API by delegation to the shared helper.
- Canonicalized generated-output signatures before snapshot creation and current-output comparison.
- Canonicalized legacy persisted stale snapshots on read/comparison, so older snapshot spelling does not require a metadata rewrite.
- Preserved malformed/non-positive fallback, semicolon ordering/deduplication, stale query purity, marker lifecycle, and current accepted numeric range.
- Added focused auto-registered Core smoke coverage in `GeneratedGeometryStaleHandleIdentitySmoke.cs` for single handles, legacy snapshots, multi-handle sets, Curtain Panel, and genuinely different handles.

## Integration evidence

- Claim reservation: `e6adf6c68599d5f4eae8c83454d8bc5deecd1a7c`
- Branch source/helper/test head: `3c7c4c3fd52f3ae139a5c33fed13342dce8d5a32`
- Reviewed PR: `#921`
- Squash merge to `main`: `9996ffb125f51b08f5e2d5ae6c6f6253f0763d8a`
- Pre-merge target blobs were re-fetched and unchanged: `ProjectElement.cs` `7b3a527b9e9d97605fa15d7e9b334ed1d39e5913`, `GeneratedHandleOwnershipPolicy.cs` `d861f87742b998dec0b1fc16c4eab3a6288e8ddb`.

## Validation boundary

Exact PR unified diff and moving-`main` target blobs were reviewed. Regression source is registered, but no GitHub Actions/build/release was dispatched and no executable Core smoke or BricsCAD V25/V26 runtime PASS is claimed.
