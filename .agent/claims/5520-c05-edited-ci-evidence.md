# Agent reservation — issue #5520

Status: ACTIVE
Reservation-Protocol: v2
Canonical owner/session: account:trinhtanphat|session:c05-automation-20260903-edited-ci-evidence
Canonical carrier: agent/c05-automation-20260903-2050/issue-5520-edited-ci-evidence
Lane-Key: issue-5520
Ownership-Key: ci.pull-request-edited-exact-head-evidence
Branch: agent/c05-automation-20260903-2050/issue-5520-edited-ci-evidence
Expected-Paths: .github/workflows/ci.yml; scripts/preflight-ci-edited-evidence.py; .agent/claims/5520-c05-edited-ci-evidence.md

Scope: preserve fail-closed PR metadata admission on pull_request edited events without cancelling or silently replacing exact-head source/build evidence; prevent redundant Core/V25 rebuilds only when prior exact-head GREEN evidence is explicitly proven.
