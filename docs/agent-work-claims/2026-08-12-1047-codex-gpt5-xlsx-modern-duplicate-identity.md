# Work claim — Modern XLSX duplicate identity refusal

- Status: `ACTIVE`
- Agent: `codex-gpt5-audit-blt-notes-latest` (`/root/audit_blt_notes_latest`)
- Registered: `2026-08-12T10:47:26+07:00`
- Baseline main SHA: `9c6164ff89456280f6a17ea4a831849f1e14e1c5`
- Priority: `P1` — modern QS3D/ED2 Excel Locate identity cells must fail closed instead of silently collapsing duplicate Element ID or CAD Handle tokens.

## Confirmed defect

`XlsxHandleReader` currently normalizes and case-insensitively deduplicates Element IDs and hexadecimal CAD Handles before enforcing the modern schema. An ED2 `CHI_TIET` cell such as `E1;e1` therefore collapses to one Element ID and passes the documented one-element check. Handle aliases such as `A;0A` likewise collapse after numeric hexadecimal normalization. Live DWG/fingerprint/provenance resolution remains guarded, but a malformed or tampered modern workbook is accepted instead of failing closed at the XLSX trust boundary.

## Reserved scope

- `src/QS3D.Core/Export/XlsxHandleReader.cs`
- one focused auto-registered Core smoke under `tests/QS3D.Core.SmokeTests/` for modern duplicate Element ID and Handle tokens
- `scripts/preflight-ed2-excel-roundtrip.py` only for the bounded static duplicate-refusal contract
- this claim file for close-out

## Intended contract

- Reject exact and case-insensitive duplicate Element ID tokens in a modern QS3D identity cell.
- Reject CAD Handle tokens that become duplicates after hexadecimal normalization, including case, `0x` and leading-zero aliases, in a modern QS3D identity cell.
- Preserve valid unique multi-handle provenance and standard BQ rows with multiple unique Element IDs.
- Preserve legacy/fuzzy Handle compatibility and existing `$decimal` deduplication; this lane does not reinterpret legacy BLT worksheets.
- Refuse malformed modern identity before any BricsCAD/CAD resolution or PICKFIRST mutation.

## Excluded scope

- No changes to `ExcelLocateResolutionService`, BricsCAD commands/UI, workbook export schemas, quantity calculation, B4D recognition, CAD Handle resolution, fingerprint policy or legacy confirmation UX.
- No private/customer fixture access, licensed BricsCAD runtime, packaging/release work or GitHub Actions dispatch.
- No edits to other XLSX exporters or current ACTIVE/BLOCKED claim surfaces.

## Validation plan

- Add deterministic Core smoke coverage for duplicate modern Element IDs and canonical Handle aliases plus valid modern and legacy controls.
- Run Core Release build/full smoke when the current shared baseline permits it.
- Run `scripts/preflight-ed2-excel-roundtrip.py`, `scripts/preflight-smoke-registration.py`, `scripts/preflight.py`, `git diff --check`, and aggregate preflight within the source-only boundary.

## Coordination

All ACTIVE/BLOCKED claims were re-read at the baseline. None reserves `XlsxHandleReader`, the proposed focused smoke, or `preflight-ed2-excel-roundtrip.py`. The active LOCAL-003 lane explicitly excludes B4D/ED2/proxy parity; BQ reconciliation excludes CAD Locate product behavior; the Material XLSX snapshot lane owns only `MaterialUsageXlsxExporter`; Core atomicity remains scoped to its persistence/session surfaces.

## Completion condition

The bounded parser/smoke/gate batch is integrated on current `main`, executed source-only evidence and any unrelated baseline blockers are recorded, the claim is closed with exact PR/commit SHAs, and no Actions/native/private operation has been performed.
