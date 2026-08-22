# Work claim — Rebar procurement CSV Unicode integrity

- Status: `ACTIVE`
- Agent: `audit-interchange-gap-next-20260815-r2`
- Registered: `2026-08-15T10:26:53+07:00`
- Baseline main SHA: `d521a3f95ee0ed80f12335e2f6affa59ce21fa9d`
- Related issue: `#84`
- Priority: remote-safe export/interchange correctness
- Claim branch: `agent/audit-interchange-gap-next/issue84-rebar-procurement-csv-unicode-claim-20260815`

## Confirmed defect

`RebarProcurementCsvExporter` writes with `new UTF8Encoding(true)`, whose replacement fallback silently converts an unpaired UTF-16 high or low surrogate to U+FFFD. `RebarStockDemand` currently permits such malformed text in `GroupId` and `Grade`, and those identities reach the public procurement CSV projection. The persisted CSV can therefore contain different procurement identity/evidence from the canonical in-memory report instead of failing closed.

## Reserved scope

- `src/QS3D.Core/Export/RebarProcurementCsvExporter.cs` — reject malformed UTF-16 in `ToCsv` and use strict BOM-bearing UTF-8 on publication; preserve projection-before-path ordering so malformed content fails before path, directory, or temporary-file side effects.
- `tests/QS3D.Core.SmokeTests/RebarProcurementCsvUnicodeIntegritySmoke.cs` — one self-registering deterministic regression for lone high/low surrogate rejection, failure atomicity, and ordinal preservation of valid supplementary Unicode plus the UTF-8 BOM.
- `scripts/preflight-rebar-procurement-csv-unicode.py` — one focused source guard for strict encoding and pre-filesystem ordering.
- this claim file for exact coordination and handoff evidence.

## Explicit exclusions

- No `RebarStockDemand`, cutting optimizer, procurement report/math, or other rebar domain changes.
- No `RebarCsvExporter`, XLSX exporter, measurement coverage exporter, semantic snapshot/import policy, IFC, BCF, native BricsCAD adapter/runtime, LOCAL probe/runner, private data, release/signing, workflow, or GitHub Actions work.
- No Unicode normalization, case, formula, schema/order, numeric-format, row-bound, or issue-state policy changes; issue `#84` remains open.

## Coordination evidence

At baseline `d521a3f95ee0ed80f12335e2f6affa59ce21fa9d`, issue `#84`, current source and history, open PR file lists, and relevant ACTIVE/BLOCKED claims were inspected. Open BCF XML/timestamp work owns different files, and the completed measurement coverage CSV Unicode lane owns only its measurement exporter/smoke/gate. No competing open PR or claim owns the exact procurement exporter/test/gate surface.

## Validation plan

- focused procurement CSV Unicode preflight plus relevant rebar/export/interchange guards;
- QS3D.Core and Core-smoke Release builds;
- full deterministic Core smoke;
- aggregate discovered feature preflights, recording unrelated failures without expanding scope;
- refresh `origin/main`, inspect final diff/ancestry, push normally, and open an implementation PR without merging `main`.

## Completion condition

Malformed UTF-16 is rejected before any filesystem side effect, valid supplementary Unicode remains ordinally identical in the BOM-bearing procurement CSV, existing formula/schema/order/numeric/10,000-row/atomic-publication contracts remain intact, remote-safe validation evidence is recorded, and the implementation is handed off in an unmerged PR while broad issue `#84` remains open.
