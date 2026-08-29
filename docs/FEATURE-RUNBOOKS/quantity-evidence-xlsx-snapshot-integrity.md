# Quantity Evidence XLSX snapshot integrity

Status: REMOTE_SAFE / Core deterministic export contract

Issue: #4560  
Lane-Key: `issue-4560`  
Canonical carrier: `agent/automation-6a815c58/issue-4560-quantity-evidence-xlsx-snapshot-integrity`

The public Issue reservation is Reservation Protocol v2 and claims exactly this exporter, its focused smoke, its auto-discovered guard, and this runbook through `Expected-Paths`. Scope expansion requires updating that same canonical reservation before mutation.

## Product boundary

`XlsxQuantityEvidenceExporter` serializes the canonical `QuantityExplanation` evidence graph into the EVIDENCE XLSX worksheet. It must not recalculate takeoff geometry or quantity arithmetic. The evidence graph remains authoritative; this boundary only makes publication deterministic when the caller supplies a mutable `IReadOnlyList<QuantityExplanation>`.

## Snapshot contract

Before projected-row capacity validation, projection, XML validation, or filesystem publication, the exporter:

1. binds the caller-owned explanation count;
2. rejects a negative or impossible-to-export explanation count;
3. reads each admitted explanation index exactly once into a detached array;
4. rejects null entries;
5. verifies the caller count stays equal to the admitted count throughout/finally;
6. performs all later capacity/projection/validation work from the detached array only.

A count change during snapshot fails closed with an export-integrity error. No temporary workbook is committed and an existing destination remains unchanged.

Count-stable replacement after an entry has already been read does not alter the detached candidate: later exporter stages never re-read the caller list. `QuantityExplanation` and its nested evidence collections are already immutable snapshots at this boundary.

## Preserved behavior

- deterministic EvidenceId ordering from `QuantityEvidenceExportProjection`;
- canonical gross/net/value, selector and operand provenance;
- Excel data-row and cell-text limits;
- malformed XML/UTF-16 rejection;
- atomic replacement/temporary-file cleanup;
- no BricsCAD/ODA runtime dependency and no `LOCAL_PASS` requirement.

## Deterministic verification

`QuantityEvidenceXlsxHardeningSmoke` includes:

- a hostile list that throws if an explanation index is read more than once, proving detached single-read materialization;
- shrink and growth cases that mutate `Count` immediately after the admitted explanation is read and require deterministic rejection before publication;
- preservation of an existing destination on count-drift rejection;
- the pre-existing Unicode, capacity, evidence-order/provenance and atomic-publication coverage.

`python scripts/preflight-quantity-evidence-xlsx-snapshot-integrity.py` is auto-discovered by the aggregate feature guard and prevents regression to the previous two-pass live caller traversal.

## Runtime classification

`NOT_APPLICABLE` for licensed BricsCAD/Excel UI. This contract is host-neutral Core serialization integrity; protected source/Core CI is the applicable acceptance evidence.
