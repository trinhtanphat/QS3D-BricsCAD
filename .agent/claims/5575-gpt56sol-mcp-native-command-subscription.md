# Agent reservation — issue #5575

Status: ACTIVE
Reservation-Protocol: v2
Canonical owner/session: account:trinhtanphat|session:gpt56sol-c04-20260904-native-event-subscriptions
Canonical carrier: agent/gpt56sol-c04-20260904-native-event-subscriptions/issue-5575-native-command-subscription-atomicity
Lane-Key: issue-5575
Ownership-Key: v25.mcp.native-command-event-subscription-atomicity
Branch: agent/gpt56sol-c04-20260904-native-event-subscriptions/issue-5575-native-command-subscription-atomicity
Expected-Paths: src/QS3D.BricsCAD.V25/McpCadMutationCoordinator.cs; scripts/preflight-mcp-native-command-subscription-atomicity.py; docs/FEATURE-RUNBOOKS/mcp-native-command-subscription-atomicity.md; .agent/claims/5575-gpt56sol-mcp-native-command-subscription.md

Scope: make native-command event subscription publication all-or-nothing. If any BricsCAD command-event add fails before `_pending` publication, detach every handler that may already have been attached before releasing process-global writer admission. No retry of native subscription and no change to terminal command semantics.
