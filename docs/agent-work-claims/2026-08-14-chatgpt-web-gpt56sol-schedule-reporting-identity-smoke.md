# Schedule reporting identity smoke alignment

- Status: ACTIVE
- Owner: ChatGPT Web / GPT-5.6 Sol
- Base main SHA: `5d4f3b2eca6b62746bdcef9b41b45df5315f2815`
- Scope: Fix the `ScheduleReportingIdentitySmoke.NoncanonicalMutableReferenceIdsFailClosed` CI regression without weakening production identity canonicalization or reporting fail-closed validation.
- Claimed file: `tests/QS3D.Core.SmokeTests/ScheduleReportingIdentitySmoke.cs`
- Expected change: Exercise public setter canonicalization, then inject raw legacy/noncanonical relation IDs through private backing fields before asserting every project report builder rejects malformed stored state.
- Exclusions: No production reporting/domain behavior changes unless a new production defect is independently proven.
- Verification: Run `QS3D.Core.SmokeTests` through the release-v25-cloud workflow on the resulting exact `main` SHA.
