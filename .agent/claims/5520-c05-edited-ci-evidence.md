# Agent reservation — issue #5520

Status: ACTIVE
Reservation-Protocol: v2
Canonical owner/session: account:trinhtanphat|session:c05-automation-20260904
Canonical carrier: agent/c05-automation-20260904/issue-5520-edited-ci-evidence-v2
Lane-Key: issue-5520
Ownership-Key: ci.pull-request-edited-exact-head-evidence
Branch: agent/c05-automation-20260904/issue-5520-edited-ci-evidence-v2
Expected-Paths: .github/workflows/ci.yml; scripts/preflight-ci-edited-evidence.py; .agent/claims/5520-c05-edited-ci-evidence.md

Scope: preserve fail-closed PR metadata admission on pull_request edited events without cancelling exact-head code validation; retain bounded cancellation inside isolated metadata/code concurrency classes, reuse prior exact-head GREEN only for source/build work, and fall back to ordinary validation when prior GREEN evidence is absent or uncertain.
