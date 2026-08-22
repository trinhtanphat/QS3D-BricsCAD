# Work claim — license signature Base64 canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-license-signature-base64-canonicality-20260812-1025`
- Registered: `2026-08-12T10:25:00+07:00`
- Baseline main SHA: `423368eb88a34e92e58a0a0afea7d50688d63fbc`
- Priority: P1 licensing fail-closed / deterministic signed-document representation.

## Confirmed defect

`LicenseVerifier.Load(...)` previously evaluated signature text with `Convert.FromBase64String((signatureElement.Value ?? string.Empty).Trim())`. `Convert.FromBase64String` accepts Base64 whitespace, and the explicit `Trim()` accepted surrounding whitespace, so multiple XML spellings mapped to the same signature bytes even though the rest of the license loader is strict about document representation.

## Implemented fix

- Decode the exact signature element text without pre-trimming.
- Re-encode decoded bytes with `Convert.ToBase64String(...)` and require exact ordinal equality with the stored text.
- Canonical signature text remains supported; surrounding or embedded whitespace forms fail closed.
- Empty-signature load behavior, maximum signature size, RSA-SHA256 policy, text-only XML policy and all signed canonical payload fields remain unchanged.

## Integration evidence

- Claim registration: `de7fae68ff0660238b55535496f2fd06b4a2aa5a`.
- Branch source commit: `a715cfe1fb1f6690dd2b90451b7d0ef5c1776504`.
- Focused smoke commit: `f8386e8443dea0a0e68fd24b6c5a7e31a5196673`.
- Exact branch diff was only `LicenseVerifier.cs` (+7/-1) plus the new 60-line smoke.
- Comparison from claim registration to PR base `f74dbc55d5c141b78d7f20d0a65bac26b901126f` showed 23 intervening commits and no licensing-source/smoke overlap.
- PR `#756` merged at `f10b1f26014b0b16ddcf3b4182d811ad8ce89e4d`.
- Post-merge main readback confirmed source blob `a63e30187043934a7d09287f30081702910412bd` and smoke blob `cc9499a062ef9f5c1b5d64d7352f21dfd6d9af27`.

## Validation boundary

Committed deterministic Core smoke coverage plus exact source/diff review/readback. No GitHub Actions were dispatched and no licensed BricsCAD runtime PASS is claimed.
