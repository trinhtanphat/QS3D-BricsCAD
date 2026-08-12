# Work claim — Reporting primary ID canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-reporting-primary-id-canonicality-20260812-0913`
- Registered: `2026-08-12T09:13:00+07:00`
- Baseline main SHA: `4fcec4dcb0261a9941bac7ab49001f13f35d0d1c`
- Priority: reporting identity integrity during owner-requested continue-all bug audit

## Confirmed defect

`ReportingProjectIdentityGuard.RequireUniqueIds(...)` rejects blank and duplicate primary IDs, but trims nonblank IDs only for duplicate comparison and otherwise accepts whitespace-padded Element/Floor/Zone/Family IDs. Reporting builders subsequently create lookup dictionaries from the raw primary IDs while normalizing references with `Trim()`. A malformed padded primary ID can therefore pass the shared guard and then silently fail reference lookup, causing report rows to fall back to the ID token instead of the canonical Floor/Family label. Persistence already treats padded primary IDs as noncanonical.

## Reserved scope

- `src/QS3D.Core/Reporting/ReportingProjectIdentityGuard.cs`
- `tests/QS3D.Core.SmokeTests/ReportingPrimaryIdCanonicalitySmoke.cs`
- this claim file for close-out

## Contract

- shared reporting identity validation rejects primary IDs whose raw text differs from its trimmed canonical form;
- keep case-insensitive duplicate detection and blank/null collection behavior unchanged;
- keep reference normalization behavior unchanged;
- add isolated deterministic Core smoke coverage proving Door/Opening schedule fails closed on malformed padded Floor/Family primary IDs while a valid project still resolves labels normally;
- no CAD mutation, persistence schema, WPF/native BricsCAD, updater/release packaging or unrelated reporting behavior changes.

No GitHub Actions dispatch and no BricsCAD V25/V26 runtime PASS claim from this web session.
