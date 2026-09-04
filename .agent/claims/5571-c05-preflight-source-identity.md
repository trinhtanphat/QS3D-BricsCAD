# Agent reservation — issue #5571

Status: ACTIVE
Reservation-Protocol: v2
Canonical owner/session: account:trinhtanphat|session:c05-automation-20260904
Canonical carrier: agent/c05-automation-20260904/issue-5571-preflight-source-identity
Lane-Key: issue-5571
Ownership-Key: ci.aggregate-preflight.source-identity-toctou
Branch: agent/c05-automation-20260904/issue-5571-preflight-source-identity
Expected-Paths: scripts/preflight-all.py; scripts/preflight-all-source-identity-smoke.py; .agent/claims/5571-c05-preflight-source-identity.md

Scope: bind auto-discovered feature preflight execution to the exact source admitted during discovery; fail closed on identity/content replacement while preserving timeout, process-tree, output, environment, ordering, and annotation contracts.
