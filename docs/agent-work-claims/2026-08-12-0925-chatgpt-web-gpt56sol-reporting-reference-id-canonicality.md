# Work claim — Reporting element reference ID canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-reporting-reference-id-canonicality-20260812-0925`
- Registered: `2026-08-12T09:25:00+07:00`
- Baseline main SHA: `d7661392c7ee4a1562d09f42104857189c1f0fd5`
- Priority: reporting relation integrity during owner-requested continue-all bug audit

## Confirmed defect

`ProjectElement.FamilyId`, `FloorId`, and `ZoneId` are publicly settable after construction. The reporting builders normalize these references with `Trim()` before lookup, so a malformed padded reference such as `" F1 "` is silently repaired during reporting instead of failing closed. The shared reporting identity boundary now rejects padded primary IDs, making silently accepting padded relation IDs inconsistent and able to hide malformed in-memory/project state.

## Reserved scope

- `src/QS3D.Core/Reporting/ReportingProjectIdentityGuard.cs`
- `tests/QS3D.Core.SmokeTests/ReportingReferenceIdCanonicalitySmoke.cs`
- this claim file for close-out

## Contract

- reporting validation rejects nonblank Element Family/Floor/Zone reference IDs whose raw value differs from its trimmed canonical value;
- blank references remain allowed where the existing domain/report contract allows an unbound relation;
- primary-ID validation, duplicate detection, lookup normalization and valid schedule labels remain unchanged;
- add isolated deterministic Core smoke coverage for padded Floor/Family/Zone references and valid canonical references;
- no CAD mutation, persistence schema, WPF/native BricsCAD, updater/release packaging or unrelated reporting behavior changes.

No GitHub Actions dispatch and no BricsCAD V25/V26 runtime PASS claim from this web session.
