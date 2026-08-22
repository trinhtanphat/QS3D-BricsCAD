# Work claim — material catalog Base64 canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-material-catalog-base64-canonicality-20260812-0819`
- Registered: `2026-08-12T08:19:00+07:00`
- Baseline main SHA: `b93aaf08119d53a5316b39864816871c6704b6fa`
- Priority: P2 — keep persisted Material Catalog metadata deterministic and fail-closed on alternate Base64 spellings.

## Confirmed defect

`ProjectMaterialCatalog.WriteCustom(...)` always emits canonical `Convert.ToBase64String(...)` fields, while `ReadCustom(...)` decoded with `Convert.FromBase64String(...)`, which accepts whitespace. Directly mutated or externally corrupted metadata could therefore use a Base64 spelling the writer never emits and still be treated as valid catalog state.

## Implemented fix

- Decoder now decodes then re-encodes each field and requires exact ordinal identity with stored text.
- Strict UTF-8 validation, decoded material trimming/length semantics, record count/empty-record rules, built-in shadowing checks, id/name uniqueness, mutation atomicity and writer output remain unchanged.
- Focused smoke verifies canonical writer-form metadata loads and a whitespace-padded Base64 field fails closed.

## Integration evidence

- Claim registration: `109dd233b9cda9694cdbd74b3446a253835ab07b`.
- Branch source commit: `b7371cd4fec9a1cc7fd6458bcdcbb49bd9750870`.
- Branch smoke commit: `9934f18ec9489808e6bf5ca6b4bc33aa6efc265b`.
- Branch diff was exactly the reserved catalog source plus new focused smoke (+8/-1 source lines).
- Comparison from claim registration to then-current `main` `cc3d339a78546ed9fa06d466f43ce24274b95115` showed 22 intervening commits and no modification of either reserved path.
- PR `#646` squash-merged cleanly at `8730813632901f453d46d76cc0222901ea605c39`.

## Validation boundary

Committed deterministic Core smoke coverage plus exact source/diff review. No GitHub Actions were dispatched and no licensed BricsCAD V25 runtime PASS is claimed.
