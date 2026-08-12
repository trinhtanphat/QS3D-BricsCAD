# Work claim — QSDB current-schema drawingFingerprint canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-qsdb-drawing-fingerprint-canonicality`
- Registered: `2026-08-12T19:06:00+07:00`
- Baseline main SHA: `0d878b7aea8d8bd4c6c5951cc44e42d357293da7`
- Priority: P0 — current-schema QSDB must reject non-canonical persisted drawing fingerprints instead of accepting leading/trailing whitespace that can later round-trip as a different persistence identity.

## Reserved scope

Harden `QsdbProjectXmlSchemaValidator.ValidateCurrent(...)` so optional `drawingFingerprint` attributes are canonical at both the project root and each element. Reuse the existing optional canonical-attribute boundary. Add one focused Core smoke regression proving whitespace-padded root/element fingerprints are rejected while canonical and empty optional fingerprints remain accepted.

## Expected surfaces

- `src/QS3D.Core/Persistence/QsdbProjectXmlSchemaValidator.cs`
- `tests/QS3D.Core.SmokeTests/QsdbDrawingFingerprintCanonicalitySmoke.cs`
- this claim file

## Excluded scope

- `drawingPath` semantics or canonicality
- fingerprint casing semantics
- schema migration/legacy compatibility
- serializer shape beyond validation of current-schema publication/load boundaries
- unrelated QSDB identity/reference invariants
- GitHub Actions or licensed BricsCAD V25 runtime qualification

## Validation plan

- root `drawingFingerprint=" DWG-X "` is rejected by current-schema validation
- element `drawingFingerprint=" DWG-X "` is rejected by current-schema validation
- canonical root/element fingerprints continue to pass
- absent/empty optional fingerprints remain valid under the existing optional-attribute contract
- read back source and smoke from integrated `main` and record exact integration ancestry

## Coordination

Current source permits `drawingFingerprint` in both allowed-attribute sets but omits the existing `ValidateOptionalCanonicalAttribute(...)` call for those attributes. Recent `main` history and the open-PR list were refreshed immediately before this claim; no active/open lane was found for this exact validator boundary.

## Completion condition

The source fix and focused smoke are integrated into current `main`, read back successfully, exact claim/source/regression/PR/integration SHAs are recorded, and this claim is updated to `COMPLETED` with only actually executed validation stated.
