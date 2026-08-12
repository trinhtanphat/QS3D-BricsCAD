# Work claim — Reporting primary ID canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-reporting-primary-id-canonicality-20260812-0913`
- Registered: `2026-08-12T09:13:00+07:00`
- Baseline main SHA: `4fcec4dcb0261a9941bac7ab49001f13f35d0d1c`
- Priority: reporting identity integrity during owner-requested continue-all bug audit

## Confirmed defect

`ReportingProjectIdentityGuard.RequireUniqueIds(...)` rejected blank and duplicate primary IDs, but trimmed nonblank IDs only for duplicate comparison and otherwise accepted whitespace-padded Element/Floor/Zone/Family IDs. Reporting builders create lookup dictionaries from raw primary IDs while normalizing references with `Trim()`, so malformed padded primary IDs could pass the shared guard and then silently miss canonical Floor/Family lookup.

## Reserved scope

- `src/QS3D.Core/Reporting/ReportingProjectIdentityGuard.cs`
- `tests/QS3D.Core.SmokeTests/ReportingPrimaryIdCanonicalitySmoke.cs`
- this claim file for close-out

## Completed implementation

- Claim registration on `main`: `8a18ecac36f4b55026dca675487452d8a7365d50`.
- Source fix: `8ecc48a4d3bd3a308c4e42b814cecea6f1b32989`.
- Focused Core smoke: `891f454b5756031610f4737851e6aeab36116a04`.
- Reconciled latest concurrent `main` without force-push: `b9202173051e3c7b03829835526420c009bf4fa2`.
- Pull request: `#688` — `fix(reporting): reject noncanonical primary ids`.
- Squash merge to `main`: `640b41b4178a290b06f45aae1b166a007cd8243b`.

## Result / evidence

- shared reporting identity validation now rejects any primary ID whose raw value differs from its trimmed canonical value before lookup dictionaries are built;
- case-insensitive duplicate detection and blank/null collection behavior remain unchanged;
- reference normalization remains unchanged;
- merged `ReportingPrimaryIdCanonicalitySmoke` proves valid Door/Opening reporting still resolves canonical Floor/Family labels and padded Floor/Family primary IDs fail closed;
- merged source and smoke were read back directly from `main` after PR integration.

No GitHub Actions were dispatched. No BricsCAD V25/V26 runtime PASS is claimed from this web session.
