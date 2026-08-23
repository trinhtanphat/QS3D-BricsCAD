# Issue #3594 — V25 modeless native document identity

Status: `SOURCE_IN_PROGRESS`

Lane-Key: `issue-3594`

Branch: `agent/web-gpt56sol-20260822-doclife1/issue-3594-native-document-identity`

Base: `main@4da9656d80da4ae59c6d8ad7e6ce31974fada07c`

## Boundary

Fix only the repository-safe V25 modeless lifetime defect reported by LOCAL-002 broad H.1: BricsCAD can raise `DocumentToBeDestroyed` with a different managed `Document` wrapper for the same native DWG. The source correction must use stable native/database identity, reject a genuinely different database, remain independent of mutable filename/path state, and preserve the existing close-once and dynamic modeless guards.

## Validation contract

Repository source/build CI is allowed to run automatically from the branch push. Do not manually dispatch Actions for this lane. Licensed BricsCAD V25 LOCAL-002 H.1 evidence remains `LOCAL_VERIFY_REQUIRED` after the source commit and must be rerun by the local agent on the exact pushed SHA.
