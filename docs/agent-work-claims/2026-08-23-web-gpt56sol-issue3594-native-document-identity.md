# Issue #3594 — V25 modeless native document identity

Status: `SOURCE_READY / PENDING_LOCAL`

Lane-Key: `issue-h1-document-wrapper-drift-source-fix`

Branch: `agent/web-gpt56sol-20260822-doclife1/issue-3594-native-document-identity`

Base: `main@4d5ffc5ad3bca0d8de9af0f45178e9eddeaa33b6`

## Boundary

Fix only the repository-safe V25 modeless lifetime defect reported by LOCAL-002 broad H.1: BricsCAD can raise `DocumentToBeDestroyed` with a different managed `Document` wrapper for the same native DWG. The source correction uses the live native database identity, rejects a genuinely different database, remains independent of mutable filename/path state, and preserves the existing close-once, project-affinity, attach-rollback, and dynamic modeless-hub guards.

## Validation contract

Repository source/build CI may run automatically from branch/PR activity. Do not manually dispatch Actions for this lane. Licensed BricsCAD V25 LOCAL-002 H.1 evidence remains `PENDING_LOCAL / DO_NOT_RETRY_REMOTE`; issue #3593 must rerun the same A/B/C H.1 P01 cell against the exact merged `main` SHA after this source fix lands. Remote/static/build evidence must not be reported as `LOCAL_PASS`.
