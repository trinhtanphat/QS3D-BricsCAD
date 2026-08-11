# Work claim — interchange export validator parity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-interchange-export-validator-parity-20260812-0117`
- Registered: `2026-08-12T01:17:00+07:00`
- Baseline main SHA: `df8ee6865e9fcd3e1b80ba6abc535098a960af03`
- Priority: evidence-driven remote-safe interchange integrity hardening during owner-requested `continue all`

## Reserved scope

Make `ProjectInterchangeJsonExporter` fail closed when its generated semantic snapshot violates the repository's canonical `ProjectInterchangeJsonValidator` contract, and ensure invalid export input is rejected before destination filesystem mutation.

## Expected surfaces

- `src/QS3D.Core/Export/ProjectInterchangeJsonExporter.cs`
- focused Core smoke coverage under `tests/QS3D.Core.SmokeTests/`

## Excluded scope

- Changes to `ProjectInterchangeJsonValidator` itself.
- Import/remap/provenance mutation policies or validated reader behavior.
- Previously completed null/duplicate-ID/semantic-reference export lanes.
- BricsCAD V25/Windows/native runtime qualification and GitHub Actions.

## Validation plan

- Preserve ordinary canonical Build output and confirm the generated JSON is validator-valid.
- Cover validator boundary parity with an accepted 512-character project name and rejection at 513 characters.
- Cover an oversized portable property value that the canonical validator rejects.
- Verify `Export()` rejects invalid content before destination directory/file creation.
- Re-read exact PR diff and moving `main` before integration; do not dispatch Actions.

## Completion condition

Exporter + focused regression are merged to current `main`, remote source is re-read, and this claim is marked `COMPLETED` with exact integration SHA and validation boundaries.
