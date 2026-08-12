# Work claim — Reporting element reference ID canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-reporting-reference-id-canonicality-20260812-0925`
- Registered: `2026-08-12T09:25:00+07:00`
- Baseline main SHA: `d7661392c7ee4a1562d09f42104857189c1f0fd5`
- Priority: reporting relation integrity during owner-requested continue-all bug audit

## Confirmed defect

`ProjectElement.FamilyId`, `FloorId`, and `ZoneId` are publicly settable after construction. Reporting builders normalized these references with `Trim()` before lookup, so a malformed padded relation ID was silently repaired during reporting instead of failing closed.

## Reserved scope

- `src/QS3D.Core/Reporting/ReportingProjectIdentityGuard.cs`
- `tests/QS3D.Core.SmokeTests/ReportingReferenceIdCanonicalitySmoke.cs`
- this claim file for close-out

## Completed implementation

- Claim registration on `main`: `1811234b6f75a955b01461cdd7cf52f52d05aa10`.
- Source fix: `e03a387ca34e01687febc4fa0d7e72f7f53e7968`.
- Focused Core smoke: `070424d12bcbcd67acef25631780d0896e9b360b`.
- Pull request: `#696` — `fix(reporting): reject noncanonical relation ids`.
- Squash merge to `main`: `7319b42d95cbf2ae15b02f564ac8a593d1d33ba5`.

## Result / evidence

- shared reporting validation now rejects whitespace-padded nonblank Element Family/Floor/Zone relation IDs before any schedule/report lookup;
- blank/unbound relation references remain allowed under the existing reporting contract;
- primary-ID validation, case-insensitive duplicate detection and lookup normalization remain unchanged;
- merged smoke covers valid canonical references, padded Floor/Family/Zone references and the existing blank-reference allowance;
- merged source and smoke were read back directly from `main` after PR integration.

No GitHub Actions were dispatched. No BricsCAD V25/V26 runtime PASS is claimed from this web session.
