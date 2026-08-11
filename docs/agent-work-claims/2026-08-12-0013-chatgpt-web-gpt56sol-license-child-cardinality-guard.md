# Work Claim: License Child Section Cardinality Guard

- Status: `ACTIVE`
- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-12
- Mode: Remote source-safe
- Scope: fail closed on duplicate semantic child sections in signed license XML while preserving current optional/missing behavior.

## Reserved files

- `src/QS3D.Core/Licensing/LicenseVerifier.cs`
- `tests/QS3D.Core.SmokeTests/LicenseVerifierSmoke.cs`
- `docs/agent-work-claims/2026-08-12-0013-chatgpt-web-gpt56sol-license-child-cardinality-guard.md`

## Boundaries

- No BricsCAD runtime/native changes.
- No licensing schema expansion beyond child-cardinality validation.
- No changes to feature canonicalization or canonical token whitespace semantics.
- No GitHub Actions dispatch.

## Validation plan

- Add deterministic smoke regression coverage for duplicate `<valid>`, `<features>`, and `<signature>` sections.
- Review the final source/test diff through the GitHub connector.
- Do not claim BricsCAD V25 runtime validation from this remote session.
