# Work claim — Reporting identity nullability build blocker

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-reporting-identity-nullability-build-20260812-0954`
- Registered: `2026-08-12T09:54:00+07:00`
- Baseline main SHA: `b59708f299d0708cbaea6a27bbe32958a143e346`
- Priority: P0 Core strict Release compile blocker
- Task Key: `CORE-REPORTING-IDENTITY-CS8602`

## Evidence

The completed XLSX negative-preflight lane recorded an actually executed strict Core Release build blocked by `CS8602` at `ReportingProjectIdentityGuard.cs:58`. Current source already rejects null project elements in `RequireUniqueIds(...)`, but `RequireCanonicalElementReferences(...)` dereferences the same nullable collection entries in a separate loop without a local guard, so nullable flow analysis cannot prove safety.

## Reserved scope

- `src/QS3D.Core/Reporting/ReportingProjectIdentityGuard.cs`
- focused source/static regression only if needed
- this claim file

## Intended fix

Make the reference-validation loop locally fail closed on null project elements before dereference, preserving the existing null diagnostic semantics, canonical relation checks, duplicate/primary identity checks and all report output behavior for valid projects.

## Coordination

Recent reporting identity/canonicality lanes are completed; no current recent claim reserves this exact file/nullability compile blocker. Do not touch XLSX/ED2 exporters, Quantity Summary, native UI, or LOCAL-003 V25 scope.

## Validation boundary

Exact source readback and ancestry verification. No GitHub Actions dispatch and no claim of full Core/BricsCAD runtime PASS unless actually executed.