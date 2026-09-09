# Coordination Issue XLSX bounded projection

Issue: #6289  
Lane: C02 Quantity / Estimating / Measurement / Excel-XLSX / Export  
Runtime: REMOTE_SAFE deterministic Core/export.

## Defect

`CoordinationIssueExcelWorkbook.Export` currently accepts almost the full Excel row ceiling, projects the lifecycle snapshot, and then materializes a complete 22-column `issueRows` graph before the temporary XLSX package exists. Per-entry, aggregate XML and final archive byte limits apply only after this graph has already been allocated, so hostile-but-valid source cardinality can amplify managed memory before bounded package writing can fail closed.

## Required production contract

1. Admit a deliberately bounded issue-export cardinality before `CoordinationIssueExcelLifecycle.Project(snapshot)`.
2. Do not materialize the complete workbook issue-row graph before ZIP creation; write/project issue rows through a bounded/streamed worksheet path.
3. Preserve deterministic projected order and `STT`, immutable issue identity/revision/CAD/provenance fields, and editable status/severity/assignee/comment import semantics.
4. Preserve strict cell/XML encoding and the existing 32 MiB entry, 64 MiB aggregate-uncompressed XML, and 64 MiB final archive budgets.
5. Reject hostile oversized export input before destination publication; preserve a pre-existing destination byte-for-byte and clean only the carrier-owned temp file.
6. Preserve normal export/import round-trip, no-op revision behavior, editable lifecycle plan behavior, and immutable-trace tamper rejection.

## Qualification

RED-first guard: `scripts/preflight-coordination-issue-xlsx-bounded-projection.py`.

After production implementation, run the focused guard, `CoordinationIssueExcelWorkbookSmoke`, broad deterministic Core smoke, package/XML hostile cases, Reservation-v2 collision gate and fresh exact-head Shared `preflight + core`. Reconcile latest protected `main` non-force before merge and integrate only with an expected-head guard.

No licensed BricsCAD runtime PASS is required or claimed for this host-neutral exporter.