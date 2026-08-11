# Work claim — door XLSX cell text limit

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-door-xlsx-cell-text-limit-20260812-0055`
- Registered: `2026-08-12T00:55:00+07:00`
- Baseline main SHA: `c6acf7a3b338cd94dc4de58103f2b141d6508490`
- Priority: evidence-driven remote-safe XLSX integrity hardening during owner-requested `continue all`

## Reserved scope

Fail closed in the Door/Opening XLSX exporter when any inline-string cell would exceed Excel's 32,767-character cell-content limit, including aggregated Element IDs and Host IDs, before any filesystem mutation.

## Expected surfaces

- `src/QS3D.Core/Export/DoorOpeningXlsxExporter.cs`
- focused Core smoke coverage under `tests/QS3D.Core.SmokeTests/`

## Excluded scope

- Shared `XlsxXmlText` behavior and other XLSX exporters.
- Door/Opening schedule grouping semantics or the completed group-key-collision lane.
- Material Usage / Room Finish / Curtain schedule work, including currently active neighboring claims.
- BricsCAD V25/Windows UI/runtime qualification and GitHub Actions.

## Validation plan

- Preserve ordinary Door/Opening XLSX export behavior below the limit.
- Cover exactly 32,767 characters as accepted and 32,768 as rejected.
- Cover aggregate `ElementIds` / `HostIds` length without allocating an oversized joined string before rejection.
- Verify rejection occurs before destination directory/file creation.
- Re-read the exact PR diff and current `main` before integration; do not dispatch Actions.

## Coordination

Recent Door schedule group-key work is completed; this claim is limited to XLSX serialization limits and does not touch report grouping. Active Room Finish/Material/Curtain and quantity/rebar lanes are explicitly excluded.

## Completion condition

Focused source + smoke regression are merged to current `main`, remote source is re-read, and this claim is marked `COMPLETED` with exact integration SHA and validation boundaries.
