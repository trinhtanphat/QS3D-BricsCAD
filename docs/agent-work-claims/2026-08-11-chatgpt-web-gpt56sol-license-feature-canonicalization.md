# Agent work claim — license feature canonicalization integrity

- Status: `RELEASED`
- Owner: ChatGPT Web / GPT-5.6 Sol
- Track: Core licensing signature canonicalization
- Mode: Remote source-safe
- Started: 2026-08-11 21:28 +07
- Completed: 2026-08-11 21:36 +07
- Baseline main SHA observed before reservation attempt: `6dc93895243bef0f26f7b5c22113977216ef38fb`
- Reservation commit: `68f97d7f77be9051345c668650f89329dbd1501f`

## Claimed paths

- `src/QS3D.Core/Licensing/LicenseVerifier.cs`
- `tests/QS3D.Core.SmokeTests/LicenseVerifierSmoke.cs`
- `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs`
- `docs/agent-work-claims/2026-08-11-chatgpt-web-gpt56sol-license-feature-canonicalization.md`

## Defect fixed

License feature names are joined with `,` in the signed canonical payload, but validation previously permitted commas inside individual feature names. Distinct feature sets could therefore collapse onto the same signed feature line. Validation now rejects the reserved comma delimiter in feature names before signing or verification.

## Test coverage

- Added a deterministic smoke regression proving delimiter-bearing feature names fail closed during canonical payload creation.
- Kept DTD rejection coverage while replacing the fixture's unnecessary external-system reference with an internal DTD entity.
- Registered the existing `LicenseVerifierSmoke` suite in `SmokeTestRegistration` so licensing signature, tamper, time-window, XML DTD and delimiter checks participate in the main smoke harness.

## Published commits

- `9bf14423186b94911ce914737b90bdfdcf11fd8e` — reject ambiguous feature delimiters.
- `4862b9d1bccef98d99858be1fb6ffb2cae71a302` — add licensing delimiter regression and keep safe DTD coverage.
- `b658935a66b4107f6a6ee4c827fb075d59ab5ae7` — register licensing smoke suite while preserving concurrent registry additions.

## Validation notes

- Exact source/test changes reviewed through the GitHub connector and all writes used current blob SHAs to reject stale overwrites.
- The execution environment does not provide `dotnet`, so the smoke executable could not be run locally in this session.
- GitHub Actions were not dispatched, consistent with repository CI policy and the absence of an explicit owner request to consume CI.

## Blocked dependencies

None.
