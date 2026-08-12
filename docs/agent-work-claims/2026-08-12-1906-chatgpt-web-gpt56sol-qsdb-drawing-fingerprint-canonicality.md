# Work claim — QSDB current-schema drawingFingerprint canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-qsdb-drawing-fingerprint-canonicality`
- Registered: `2026-08-12T19:06:00+07:00`
- Completed: `2026-08-12T19:45:00+07:00`
- Baseline main SHA: `0d878b7aea8d8bd4c6c5951cc44e42d357293da7`
- Priority: P0 — current-schema QSDB must reject non-canonical persisted drawing fingerprints instead of accepting leading/trailing whitespace that can later round-trip as a different persistence identity.

## Reserved scope

Harden `QsdbProjectXmlSchemaValidator.ValidateCurrent(...)` so optional `drawingFingerprint` attributes are canonical at both the project root and each element. Reuse the existing optional canonical-attribute boundary. Add one focused Core smoke regression proving whitespace-padded root/element fingerprints are rejected while canonical fingerprints remain accepted.

## Integrated surfaces

- `src/QS3D.Core/Persistence/QsdbProjectXmlSchemaValidator.cs`
- `tests/QS3D.Core.SmokeTests/QsdbDrawingFingerprintCanonicalitySmoke.cs`
- `tests/QS3D.Core.SmokeTests/QsdbDrawingFingerprintCanonicalityRegistration.cs`
- this claim file

## Excluded scope

- `drawingPath` semantics or canonicality
- fingerprint casing semantics
- schema migration/legacy compatibility
- serializer shape beyond validation of current-schema publication/load boundaries
- unrelated QSDB identity/reference invariants
- GitHub Actions or licensed BricsCAD V25 runtime qualification

## Integration record

- Claim: `ebb95121ebe6865abaacac0649ca96880a941e3d`
- Source fix: `7cc7e1c6e1c98424f6928c80b48ba884ed5c492f`
- Focused smoke: `f69491d09ee52e4dc30e7dd202252f0adc2e723e`
- Smoke registration: `a49c5bab746e3a0aa5c9e12cf8b81839c1075e39`
- Pull request: `#944`
- Squash integration: `c5a7b8f643fc4649d99465d6aa8f2b40453fc52f`

## Validation performed

- Source commit compare read-back confirmed exactly two validator additions and no deletions.
- PR patch read-back confirmed exactly the validator change plus the focused smoke and module-initializer registration.
- GitHub reported PR `#944` mergeable before squash integration.
- Integrated `main` was read back at `c5a7b8f643fc4649d99465d6aa8f2b40453fc52f`; both project-root and element `drawingFingerprint` canonical guards are present.
- Integrated smoke was read back from `main`; it covers padded root rejection, padded element rejection, and canonical root/element round-trip through `QsdbProjectStore.SaveNew/Load`.
- Existing optional canonical helper semantics still allow absent/empty values and reject whitespace-only or padded non-empty values.
- No GitHub Actions run, local `dotnet` execution, or licensed BricsCAD V25 runtime PASS is claimed by this closeout.

## Completion

The defect is integrated into `main` with focused regression coverage and source/smoke read-back. This lane is closed as `COMPLETED`.
